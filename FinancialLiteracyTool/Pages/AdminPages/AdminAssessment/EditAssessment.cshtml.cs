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
    public class EditAssessmentModel : PageModel
    {
        private const int MaxAssessmentNameLength = 100;
        public bool IsAdmin { get; set; }

        public MyAssessment EditAssessment { get; set; } = new();

        // Areas multi-select
        public List<SelectListItem> AreaSelectList { get; set; } = new();
        public List<int> SelectedAreaIDs { get; set; } = new();

        // New: question-count selector + preview support (auto-generate only)
        public int SelectedQuestionCount { get; set; }
        public List<int> SelectedQuestionIDs { get; set; } = new();
        public List<QuestionViewModel> PreviewQuestions { get; set; } = new();

        // Accept id as query or route parameter
        public IActionResult OnGet(int? id)
        {
            // populate areas so multi-select is available and preselected areas can be set
            PopulateAreaSelectList();

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                CheckIfUserIsAdmin(int.Parse(userIdClaim.Value));
            }

            if (!IsAdmin)
            {
                return Forbid();
            }

            if (!id.HasValue)
            {
                return Page();
            }

            LoadAssessment(id.Value);
            return Page();
        }

        // Save name/description and optionally apply generated question selection
        public IActionResult OnPost()
        {
            // ensure area list is present when redisplaying the page
            PopulateAreaSelectList();

            if (!IsAdmin)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null) CheckIfUserIsAdmin(int.Parse(userIdClaim.Value));
                if (!IsAdmin) return Forbid();
            }

            if (!string.IsNullOrEmpty(EditAssessment.AssessmentName) && EditAssessment.AssessmentName.Length > MaxAssessmentNameLength)
            {
                ModelState.AddModelError("EditAssessment.AssessmentName", "Assessment name too long. Try again.");
                return Page();
            }

            if (!ModelState.IsValid) return Page();

            using var conn = new SqlConnection(AppHelper.GetDBConnectionString());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // Update name/description
                using var updateCmd = new SqlCommand(
                    "UPDATE Assessment SET AssessmentName = @AssessmentName, AssessmentDescription = @AssessmentDescription WHERE AssessmentID = @AssessmentID",
                    conn, tx);
                updateCmd.Parameters.AddWithValue("@AssessmentName", EditAssessment.AssessmentName ?? string.Empty);
                updateCmd.Parameters.AddWithValue("@AssessmentDescription", EditAssessment.AssessmentDescription ?? string.Empty);
                updateCmd.Parameters.AddWithValue("@AssessmentID", EditAssessment.AssessmentID);
                updateCmd.ExecuteNonQuery();

                // If there are generated/selected questions, replace the links
                if (SelectedQuestionIDs != null && SelectedQuestionIDs.Any())
                {
                    using var delQ = new SqlCommand("DELETE FROM AssessmentQuestion WHERE AssessmentID = @AssessmentID", conn, tx);
                    delQ.Parameters.AddWithValue("@AssessmentID", EditAssessment.AssessmentID);
                    delQ.ExecuteNonQuery();

                    using var insQ = new SqlCommand("INSERT INTO AssessmentQuestion (AssessmentID, QuestionID) VALUES (@AssessmentID, @QuestionID)", conn, tx);
                    insQ.Parameters.AddWithValue("@AssessmentID", EditAssessment.AssessmentID);
                    insQ.Parameters.Add(new SqlParameter("@QuestionID", System.Data.SqlDbType.Int));
                    foreach (var qid in SelectedQuestionIDs.Distinct())
                    {
                        insQ.Parameters["@QuestionID"].Value = qid;
                        insQ.ExecuteNonQuery();
                    }
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }

            return RedirectToPage("BrowseAssessments");
        }

        // Generate preview (does not persist)
        public IActionResult OnPostGenerate()
        {
            // ensure area list is present when redisplaying the page
            PopulateAreaSelectList();

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null) CheckIfUserIsAdmin(int.Parse(userIdClaim.Value));
            if (!IsAdmin) return Forbid();

            var allowed = new[] { 10, 20, 30, 40, 50 };
            if (!allowed.Contains(SelectedQuestionCount))
            {
                ModelState.AddModelError(nameof(SelectedQuestionCount), "Select a valid number of questions.");
                return Page();
            }

            PopulateSelectedQuestionIDs();
            if (!ModelState.IsValid) return Page();

            PopulatePreviewQuestions();
            return Page();
        }

        // --- helpers: same strategy as AddAssessment ---

        private void PopulateAreaSelectList()
        {
            AreaSelectList.Clear();
            using var conn = new SqlConnection(AppHelper.GetDBConnectionString());
            using var cmd = new SqlCommand("SELECT AreaID, AreaName FROM Area ORDER BY AreaName", conn);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var id = reader.IsDBNull(0) ? (string?)null : reader.GetInt32(0).ToString();
                var name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                if (id != null)
                {
                    AreaSelectList.Add(new SelectListItem { Value = id, Text = name });
                }
            }
        }

        private void PopulateSelectedQuestionIDs()
        {
            SelectedQuestionIDs.Clear();

            if (SelectedQuestionCount <= 0)
            {
                ModelState.AddModelError(nameof(SelectedQuestionCount), "Select a number of questions.");
                return;
            }

            var areaQuestions = new Dictionary<int, List<int>>();
            using (var conn = new SqlConnection(AppHelper.GetDBConnectionString()))
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

            // If user selected specific areas, restrict to those areas (but only if they actually contain questions)
            Dictionary<int, List<int>> available = areaQuestions;
            if (SelectedAreaIDs != null && SelectedAreaIDs.Any())
            {
                var restricted = areaQuestions
                    .Where(kvp => SelectedAreaIDs.Contains(kvp.Key) && kvp.Value.Count > 0)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                if (!restricted.Any())
                {
                    ModelState.AddModelError(string.Empty, "No questions available in the selected areas. Choose different areas or leave area selection empty.");
                    return;
                }
                available = restricted;
            }

            int totalAvailable = available.Values.Sum(l => l.Count);
            if (totalAvailable < SelectedQuestionCount)
            {
                ModelState.AddModelError(string.Empty, $"Not enough questions available ({totalAvailable}) to satisfy the requested count ({SelectedQuestionCount}).");
                return;
            }

            var rng = new Random();
            var allAreaIds = available.Keys.ToList();
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
                picks[areaId] = Math.Min(assign, available[areaId].Count);
            }

            int allocated = picks.Values.Sum();
            int deficit = SelectedQuestionCount - allocated;

            if (deficit > 0)
            {
                foreach (var areaId in chosenAreas)
                {
                    int availableCount = available[areaId].Count - picks[areaId];
                    if (availableCount <= 0) continue;
                    int take = Math.Min(availableCount, deficit);
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
                    int take = Math.Min(available[areaId].Count, deficit);
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
                    int availableCount = available[areaId].Count - currently;
                    if (availableCount <= 0) continue;
                    int take = Math.Min(availableCount, deficit);
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
                var pool = available[areaId];
                var selected = pool.OrderBy(_ => rng.Next()).Take(countToTake).ToList();
                SelectedQuestionIDs.AddRange(selected);
            }

            SelectedQuestionIDs = SelectedQuestionIDs.Distinct().Take(SelectedQuestionCount).ToList();
        }

        private void PopulatePreviewQuestions()
        {
            PreviewQuestions.Clear();
            if (SelectedQuestionIDs == null || SelectedQuestionIDs.Count == 0) return;

            using var conn = new SqlConnection(AppHelper.GetDBConnectionString());
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

        private void LoadAssessment(int id)
        {
            using var conn = new SqlConnection(AppHelper.GetDBConnectionString());
            conn.Open();

            using (var cmd = new SqlCommand("SELECT AssessmentID, AssessmentName, AssessmentDescription FROM Assessment WHERE AssessmentID = @AssessmentID", conn))
            {
                cmd.Parameters.AddWithValue("@AssessmentID", id);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        EditAssessment.AssessmentID = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        EditAssessment.AssessmentName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                        EditAssessment.AssessmentDescription = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                    }
                }
            }

            // load existing associated questions
            using var qCmd = new SqlCommand("SELECT QuestionID FROM AssessmentQuestion WHERE AssessmentID = @AssessmentID", conn);
            qCmd.Parameters.AddWithValue("@AssessmentID", id);
            using (var qReader = qCmd.ExecuteReader())
            {
                SelectedQuestionIDs.Clear();
                while (qReader.Read())
                {
                    if (!qReader.IsDBNull(0)) SelectedQuestionIDs.Add(qReader.GetInt32(0));
                }
            } // qReader disposed here

            // now safe to call PopulateAreaSelectList() and run other commands on `conn`
            PopulateAreaSelectList();

            if (SelectedQuestionIDs.Any())
            {
                var parameters = SelectedQuestionIDs.Select((qid, idx) => $"@p{idx}").ToList();
                string inClause = string.Join(",", parameters);
                string sql = $@"SELECT DISTINCT AreaID FROM Question WHERE QuestionID IN ({inClause}) AND AreaID IS NOT NULL";

                using var areaCmd = new SqlCommand(sql, conn);
                for (int i = 0; i < SelectedQuestionIDs.Count; i++)
                {
                    areaCmd.Parameters.AddWithValue($"@p{i}", SelectedQuestionIDs[i]);
                }

                using var areaReader = areaCmd.ExecuteReader();
                var selAreas = new HashSet<int>();
                while (areaReader.Read())
                {
                    if (!areaReader.IsDBNull(0)) selAreas.Add(areaReader.GetInt32(0));
                }

                SelectedAreaIDs = selAreas.ToList();
            }
        }

        private void CheckIfUserIsAdmin(int userId)
        {
            using var conn = new SqlConnection(AppHelper.GetDBConnectionString());
            using var cmd = new SqlCommand("SELECT SystemUserRole FROM SystemUser WHERE SystemUserID = @SystemUserID", conn);
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