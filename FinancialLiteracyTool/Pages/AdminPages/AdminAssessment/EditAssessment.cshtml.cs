using System.Data;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using FinancialLiteracyTool.MyAppHelper;
using FinancialLiteracyTool.Model.Assessments;
using Microsoft.Data.SqlClient;

namespace FinancialLiteracyTool.Pages.AdminPages.AdminAssessment
{
    
    public class EditAssessmentModel : PageModel
    {
        private const int MaxAssessmentNameLength = 10;
        public bool IsAdmin { get; set; }

        [BindProperty]
        public MyAssessment EditAssessment { get; set; } = new();
        public List<SelectListItem> AssessmentArea { get; set; } = new();

        [BindProperty]
        public int? SelectedAssessmentAreaID { get; set; }

        [BindProperty]
        public List<int> SelectedQuestionIDs { get; set; } = new();
        public List<SelectListItem> QuestionOptions { get; set; } = new();

        // Accept id as query or route parameter
        public void OnGet(int? id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value);
                CheckIfUserIsAdmin(userId);
            }

            PopulateAssessmentAreaList();

            if (!id.HasValue)
            {
                // No id supplied - nothing to load
                return;
                // Alternatively, could redirect to BrowseAssessments or show an error
                // return RedirectToPage("BrowseAssessments");
            }

            LoadAssessment(id.Value);

            if (SelectedAssessmentAreaID.HasValue)
            {
                PopulateQuestionAreaList(SelectedAssessmentAreaID.Value);
            }
        }

        public IActionResult OnPost()
        {
            //Server-side check for length to provide immediate, friendly feedback.
            if(!string.IsNullOrEmpty(EditAssessment.AssessmentName) && EditAssessment.AssessmentName.Length > MaxAssessmentNameLength)
            {
                ModelState.AddModelError("EditAssessment.AssessmentName", "Assessment name too long. Try again.");

                // Prepare the UI to be returned so user can immediately retry
                PopulateAssessmentAreaList();
                if (SelectedAssessmentAreaID.HasValue) PopulateQuestionAreaList(SelectedAssessmentAreaID.Value);

                // set a flag so the input receives focus when the page reloads
                ViewData["FocusAssessmentName"] = true;

                return Page();
            }

            if (!ModelState.IsValid)
            {
                PopulateAssessmentAreaList();
                if (SelectedAssessmentAreaID.HasValue) PopulateQuestionAreaList(SelectedAssessmentAreaID.Value);
                return Page();
            }

            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                conn.Open();
                using var tx = conn.BeginTransaction();

                try
                {
                    // Update Assessment name
                    using (SqlCommand updateCmd = new SqlCommand(
                               "UPDATE Assessment SET AssessmentName = @AssessmentName WHERE AssessmentID = @AssessmentID",
                               conn, tx))
                    {
                        updateCmd.Parameters.Add("@AssessmentName", SqlDbType.NVarChar, MaxAssessmentNameLength)
                                 .Value = EditAssessment.AssessmentName ?? string.Empty;
                        updateCmd.Parameters.Add("@AssessmentID", SqlDbType.Int).Value = EditAssessment.AssessmentID;
                        updateCmd.ExecuteNonQuery();
                    }

                    // Replace AssessmentArea entries (existing pattern in project is single area per assessment)
                    using (SqlCommand delArea = new SqlCommand("DELETE FROM AssessmentArea WHERE AssessmentID = @AssessmentID", conn, tx))
                    {
                        delArea.Parameters.AddWithValue("@AssessmentID", EditAssessment.AssessmentID);
                        delArea.ExecuteNonQuery();
                    }

                    if (SelectedAssessmentAreaID.HasValue)
                    {
                        using (SqlCommand insArea = new SqlCommand(
                                   "INSERT INTO AssessmentArea (AssessmentID, AreaID) VALUES (@AssessmentID, @AreaID)",
                                   conn, tx))
                        {
                            insArea.Parameters.AddWithValue("@AssessmentID", EditAssessment.AssessmentID);
                            insArea.Parameters.AddWithValue("@AreaID", SelectedAssessmentAreaID.Value);
                            insArea.ExecuteNonQuery();
                        }
                    }

                    // Replace AssessmentQuestion entries
                    using (SqlCommand delQ = new SqlCommand("DELETE FROM AssessmentQuestion WHERE AssessmentID = @AssessmentID", conn, tx))
                    {
                        delQ.Parameters.AddWithValue("@AssessmentID", EditAssessment.AssessmentID);
                        delQ.ExecuteNonQuery();
                    }

                    if (SelectedQuestionIDs != null && SelectedQuestionIDs.Any())
                    {
                        using SqlCommand insQ = new SqlCommand(
                            "INSERT INTO AssessmentQuestion (AssessmentID, QuestionID) VALUES (@AssessmentID, @QuestionID)",
                            conn, tx);
                        insQ.Parameters.Add("@AssessmentID", SqlDbType.Int).Value = EditAssessment.AssessmentID;
                        insQ.Parameters.Add("@QuestionID", SqlDbType.Int);

                        foreach (var qid in SelectedQuestionIDs)
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
            }

            return RedirectToPage("BrowseAssessments");
        }
        
        // This handler is triggered by the onchange event of the AssessmentArea dropdown.
        public IActionResult OnPostLoadQuestions()
        {
            // Repopulate the area dropdown and question list for the selected area 
            // return the same page once dropdown was chosen for user to see updated questions
            PopulateAssessmentAreaList();

            if (SelectedAssessmentAreaID.HasValue)
            {
                PopulateQuestionAreaList(SelectedAssessmentAreaID.Value);
            }
            return Page();
        }
        

        private void LoadAssessment(int id)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                conn.Open();

                // Load Assessment basic info
                using (SqlCommand cmd = new SqlCommand("SELECT AssessmentID, AssessmentName FROM Assessment WHERE AssessmentID = @AssessmentID", conn))
                {
                    cmd.Parameters.AddWithValue("@AssessmentID", id);
                    using var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        EditAssessment.AssessmentID = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        EditAssessment.AssessmentName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                    }
                }

                // Load AssessmentArea (first area)
                using (SqlCommand aCmd = new SqlCommand("SELECT TOP 1 AreaID FROM AssessmentArea WHERE AssessmentID = @AssessmentID", conn))
                {
                    aCmd.Parameters.AddWithValue("@AssessmentID", id);
                    var result = aCmd.ExecuteScalar();
                    if (result != null && int.TryParse(result.ToString(), out var areaId))
                    {
                        SelectedAssessmentAreaID = areaId;
                    }
                }

                // Load selected question IDs
                using (SqlCommand qCmd = new SqlCommand("SELECT QuestionID FROM AssessmentQuestion WHERE AssessmentID = @AssessmentID", conn))
                {
                    qCmd.Parameters.AddWithValue("@AssessmentID", id);
                    using var reader = qCmd.ExecuteReader();
                    SelectedQuestionIDs.Clear();
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(0)) SelectedQuestionIDs.Add(reader.GetInt32(0));
                    }
                }
            }
        }

        private void PopulateQuestionAreaList(int id)
        {
            QuestionOptions.Clear();
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT QuestionID, QuestionText FROM Question WHERE AreaID = @AreaID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@AreaID", id);
                conn.Open();
                using var reader = cmd.ExecuteReader();
  
                while (reader.Read())
                    {
                        QuestionOptions.Add(new SelectListItem
                        {
                            Value = reader["QuestionID"].ToString() ?? "",
                            Text = reader["QuestionText"].ToString() ?? ""
                        });
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
                using var reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        AssessmentArea.Add(new SelectListItem
                        {
                            Value = reader["AreaID"].ToString() ?? "",
                            Text = reader["AreaName"].ToString() ?? ""
                        });
                    }
                }
            }
        }

        private void CheckIfUserIsAdmin(int userId)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = "SELECT IsAdmin FROM SystemUser WHERE SystemUserID = @SystemUserID";
                SqlCommand cmd = new SqlCommand(cmdText, conn);
                cmd.Parameters.AddWithValue("@SystemUserID", userId);
                conn.Open();
                var result = cmd.ExecuteScalar();

                if (result != null && result.ToString() == "True")
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