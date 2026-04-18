using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using FinancialLiteracyTool.Model.Assessments;
using FinancialLiteracyTool.Model.Questions;
using FinancialLiteracyTool.MyAppHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;

namespace FinancialLiteracyTool.Pages.AdminPages.AdminAssessment
{
    [Authorize]
    [BindProperties]
    public class AddAssessmentModel : PageModel
    {
        public bool IsAdmin { get; set; }
        public MyAssessment NewAssessment { get; set; } = new MyAssessment();

        // kept for potential informational use, not used as dropdown in view any more
        public List<SelectListItem> AssessmentArea { get; set; } = new List<SelectListItem>();

        // Question-count selector
        public int SelectedQuestionCount { get; set; }

        // Will be filled by server-side algorithm and must bind on post (hidden inputs)
        public List<int> SelectedQuestionIDs { get; set; } = new();

        public List<SelectListItem> QuestionOptions { get; set; } = new();

        // Preview objects to show text + area name
        public List<QuestionViewModel> PreviewQuestions { get; set; } = new();

        public IActionResult OnGet(int? areaId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                CheckIfUserIsAdmin(int.Parse(userIdClaim.Value));
            }

            if (!IsAdmin)
            {
                return Forbid();
            }

            PopulateAssessmentAreaList();
            return Page();
        }

        // Keep default OnPost harmless so accidental submits (enter key) don't persist directly.
        public IActionResult OnPost()
        {
            // simply redisplay the page (use Generate / Confirm handlers instead)
            PopulateAssessmentAreaList();
            return Page();
        }

        // Generate preview (does not persist). Triggered by button with formaction="?handler=Generate"
        public IActionResult OnPostGenerate()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                CheckIfUserIsAdmin(int.Parse(userIdClaim.Value));
            }

            if (!IsAdmin)
            {
                return Forbid();
            }

            var allowed = new[] { 10, 20, 30, 40, 50 };
            if (!allowed.Contains(SelectedQuestionCount))
            {
                ModelState.AddModelError(nameof(SelectedQuestionCount), "Select a valid number of questions.");
                PopulateAssessmentAreaList();
                return Page();
            }

            PopulateSelectedQuestionIDs();

            if (!ModelState.IsValid)
            {
                PopulateAssessmentAreaList();
                return Page();
            }

            // Load question texts for preview
            PopulatePreviewQuestions();

            PopulateAssessmentAreaList();
            return Page();
        }

        // Confirm & save (triggered by formaction="?handler=Confirm"). SelectedQuestionIDs expected from hidden inputs in the preview.
        public IActionResult OnPostConfirm()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                CheckIfUserIsAdmin(int.Parse(userIdClaim.Value));
            }

            if (!IsAdmin)
            {
                return Forbid();
            }

            if (SelectedQuestionIDs == null || SelectedQuestionIDs.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "No questions selected. Generate a preview first.");
                PopulateAssessmentAreaList();
                PopulatePreviewQuestions();
                return Page();
            }

            // persist assessment and its question links
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                conn.Open();
                string insertAssessmentSql = "INSERT INTO Assessment (AssessmentName, AssessmentDescription) VALUES (@AssessmentName, @AssessmentDescription);";
                using (var cmd = new SqlCommand(insertAssessmentSql, conn))
                {
                    cmd.Parameters.AddWithValue("@AssessmentName", NewAssessment.AssessmentName ?? string.Empty);
                    cmd.Parameters.AddWithValue("@AssessmentDescription", NewAssessment.AssessmentDescription ?? string.Empty);
                    cmd.ExecuteNonQuery();
                }

                int newAssessmentId;
                using (var idCmd = new SqlCommand("SELECT @@IDENTITY;", conn))
                {
                    newAssessmentId = Convert.ToInt32(idCmd.ExecuteScalar());
                }

                string insertQSql = "INSERT INTO AssessmentQuestion (AssessmentID, QuestionID) VALUES (@AssessmentID, @QuestionID);";
                foreach (var qid in SelectedQuestionIDs.Distinct())
                {
                    using var insertQ = new SqlCommand(insertQSql, conn);
                    insertQ.Parameters.AddWithValue("@AssessmentID", newAssessmentId);
                    insertQ.Parameters.AddWithValue("@QuestionID", qid);
                    insertQ.ExecuteNonQuery();
                }
            }

            return RedirectToPage("BrowseAssessments");
        }

        // --- helper methods ---

        private void PopulateSelectedQuestionIDs()
        {
            SelectedQuestionIDs.Clear();

            if (SelectedQuestionCount <= 0)
            {
                ModelState.AddModelError(nameof(SelectedQuestionCount), "Select a number of questions.");
                return;
            }

            // Load questions grouped by area
            var areaQuestions = new Dictionary<int, List<int>>();
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                const string q = "SELECT AreaID, QuestionID FROM Question";
                using var cmd = new SqlCommand(q, conn);
                conn.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (reader.IsDBNull(0) || reader.IsDBNull(1)) continue;
                    int areaId = reader.GetInt32(0);
                    int qid = reader.GetInt32(1);
                    if (!areaQuestions.ContainsKey(areaId)) areaQuestions[areaId] = new List<int>();
                    areaQuestions[areaId].Add(qid);
                }
            }

            int totalAvailable = areaQuestions.Values.Sum(l => l.Count);
            if (totalAvailable < SelectedQuestionCount)
            {
                ModelState.AddModelError(string.Empty, $"Not enough questions available ({totalAvailable}) to satisfy the requested count ({SelectedQuestionCount}).");
                return;
            }

            var rng = new Random();
            var allAreaIds = areaQuestions.Keys.ToList();
            int nAreas = allAreaIds.Count;

            int baseAreas = Math.Clamp(SelectedQuestionCount / 10, 1, nAreas);
            int extra = rng.Next(0, 2);
            int desiredAreas = Math.Min(nAreas, Math.Max(1, baseAreas + extra));

            var shuffledAreas = allAreaIds.OrderBy(_ => rng.Next()).ToList();
            var chosenAreas = shuffledAreas.Take(desiredAreas).ToList();

            var picks = new Dictionary<int, int>();
            int basePerArea = SelectedQuestionCount / chosenAreas.Count;
            int remainder = SelectedQuestionCount % chosenAreas.Count;

            foreach (var areaId in chosenAreas)
            {
                int assign = basePerArea + (remainder > 0 ? 1 : 0);
                if (remainder > 0) remainder--;
                picks[areaId] = Math.Min(assign, areaQuestions[areaId].Count);
            }

            int allocated = picks.Values.Sum();
            int deficit = SelectedQuestionCount - allocated;

            if (deficit > 0)
            {
                foreach (var areaId in chosenAreas)
                {
                    int available = areaQuestions[areaId].Count - picks[areaId];
                    if (available <= 0) continue;
                    int take = Math.Min(available, deficit);
                    picks[areaId] += take;
                    deficit -= take;
                    if (deficit == 0) break;
                }
            }

            if (deficit > 0)
            {
                var remainingAreas = shuffledAreas.Except(chosenAreas).ToList();
                foreach (var areaId in remainingAreas)
                {
                    int take = Math.Min(areaQuestions[areaId].Count, deficit);
                    if (take <= 0) continue;
                    picks[areaId] = take;
                    deficit -= take;
                    if (deficit == 0) break;
                }
            }

            if (deficit > 0)
            {
                foreach (var areaId in shuffledAreas)
                {
                    int currently = picks.ContainsKey(areaId) ? picks[areaId] : 0;
                    int available = areaQuestions[areaId].Count - currently;
                    if (available <= 0) continue;
                    int take = Math.Min(available, deficit);
                    picks[areaId] = currently + take;
                    deficit -= take;
                    if (deficit == 0) break;
                }
            }

            foreach (var kv in picks)
            {
                int areaId = kv.Key;
                int countToTake = kv.Value;
                if (countToTake <= 0) continue;

                var pool = areaQuestions[areaId];
                var selected = pool.OrderBy(_ => rng.Next()).Take(countToTake).ToList();
                SelectedQuestionIDs.AddRange(selected);
            }

            SelectedQuestionIDs = SelectedQuestionIDs.Distinct().Take(SelectedQuestionCount).ToList();
        }

        private void PopulatePreviewQuestions()
        {
            PreviewQuestions.Clear();
            if (SelectedQuestionIDs == null || SelectedQuestionIDs.Count == 0) return;

            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                conn.Open();

                var parameters = SelectedQuestionIDs.Select((id, idx) => $"@p{idx}").ToList();
                string inClause = string.Join(",", parameters);
                string sql = $@"
                    SELECT q.QuestionID, q.QuestionText, q.AreaID, ISNULL(a.AreaName, '') AS AreaName, q.QuestionTypeID
                    FROM Question q
                    LEFT JOIN Area a ON q.AreaID = a.AreaID
                    WHERE q.QuestionID IN ({inClause})
                    ORDER BY q.QuestionID";

                using var cmd = new SqlCommand(sql, conn);
                for (int i = 0; i < SelectedQuestionIDs.Count; i++)
                {
                    cmd.Parameters.AddWithValue($"@p{i}", SelectedQuestionIDs[i]);
                }

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    PreviewQuestions.Add(new QuestionViewModel
                    {
                        QuestionID = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                        QuestionText = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                        AreaID = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                        AreaName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        QuestionTypeID = reader.IsDBNull(4) ? 0 : reader.GetInt32(4)
                    });
                }
            }
        }

        private void PopulateQuestionAreaList(int? id)
        {
            if (!id.HasValue) return;
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT QuestionID, QuestionText FROM Question WHERE AreaID = @AreaID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@AreaID", id.Value);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        QuestionOptions.Add(new SelectListItem
                        {
                            Value = reader["QuestionID"].ToString(),
                            Text = reader["QuestionText"].ToString()
                        });
                    }
                }
            }
        }

        private void PopulateAssessmentAreaList()
        {
            AssessmentArea.Clear();
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT AreaID, AreaName FROM Area";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        AssessmentArea.Add(new SelectListItem
                        {
                            Value = reader["AreaID"].ToString(),
                            Text = reader["AreaName"].ToString()
                        });
                    }
                }
            }
        }

        private void CheckIfUserIsAdmin(int userId)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = "SELECT SystemUserRole FROM SystemUser WHERE SystemUserID = @SystemUserID";
                SqlCommand cmd = new SqlCommand(cmdText, conn);
                cmd.Parameters.AddWithValue("@SystemUserID", userId);
                conn.Open();
                var result = cmd.ExecuteScalar();

                if (result != null && int.TryParse(result.ToString(), out var roleValue) && roleValue == 3)
                {
                    IsAdmin = true;
                    ViewData["IsAdmin"] = true;
                }
                else
                {
                    IsAdmin = false;
                }
            }
        }
    }
}
