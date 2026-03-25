using FinancialLiteracyTool.Model.Assessments;
using FinancialLiteracyTool.Model.Questions;
using FinancialLiteracyTool.MyAppHelper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace FinancialLiteracyTool.Pages.AdminPages.AdminAssessment
{
    [BindProperties]
    public class AddAssessmentModel : PageModel
    {
        public bool IsAdmin { get; set; }
        public MyAssessment NewAssessment { get; set; } = new MyAssessment();
        public List<SelectListItem> AssessmentArea { get; set; } = new List<SelectListItem>();
        public int? SelectedAssessmentAreaID { get; set; }

        public List<int> SelectedQuestionIDs { get; set; } = new();
        public List<SelectListItem> QuestionOptions { get; set; } = new();
        public void OnGet(int? areaId)
        {

            // Safely access the NameIdentifier claim
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            /*--------------------ADMIN PRIV----------------------*/
            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value); // Use the claim value only if it exists
                CheckIfUserIsAdmin(userId);
            }
            /*--------------------ADMIN PRIV----------------------*/


            PopulateAssessmentAreaList();

            SelectedAssessmentAreaID = areaId;

            PopulateQuestionAreaList(areaId);
        }//End of 'OnGet'.

        public IActionResult OnPost()
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {

                    conn.Open();
                    string insertcmdText = "INSERT INTO Assessment (AssessmentName) VALUES (@AssessmentName);";
                    SqlCommand insertcmd = new SqlCommand(insertcmdText, conn);
                    insertcmd.Parameters.AddWithValue("@AssessmentName", NewAssessment.AssessmentName);

                    insertcmd.ExecuteScalar();

                    int newAssessmentId;
                    // fetch the generated AutoNumber (RecapID)
                    using (var idCmd = new SqlCommand("SELECT @@IDENTITY;", conn))
                    {
                        newAssessmentId = Convert.ToInt32(idCmd.ExecuteScalar());
                        NewAssessment.AssessmentID = newAssessmentId;
                    }

                    string insertcmdText2 = "INSERT INTO AssessmentArea (AssessmentID, AreaID) VALUES (@AssessmentID, @AreaID);";
                    SqlCommand insertcmd2 = new SqlCommand(insertcmdText2, conn);
                    insertcmd2.Parameters.AddWithValue("@AssessmentID", newAssessmentId);
                    insertcmd2.Parameters.AddWithValue("@AreaID", SelectedAssessmentAreaID);

                    insertcmd2.ExecuteNonQuery();

                    string insertcmdText3 =
                        "INSERT INTO AssessmentQuestion (AssessmentID, QuestionID) VALUES (@AssessmentID, @QuestionID);";

                    foreach (int questionId in SelectedQuestionIDs)
                    {
                        using SqlCommand insertcmd3 = new SqlCommand(insertcmdText3, conn);
                        insertcmd3.Parameters.AddWithValue("@AssessmentID", newAssessmentId);
                        insertcmd3.Parameters.AddWithValue("@QuestionID", questionId);
                        insertcmd3.ExecuteNonQuery();
                    }

                }
                return RedirectToPage("BrowseAssessments");
            }
            else
            {

                // If the model state is not valid, return to the same page with validation errors
                return Page();
            }
        }// End of 'OnPost'.

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
        }//End of 'PopulateQuestionAreaList'.

        private void PopulateAssessmentAreaList()
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT * FROM Area";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var area = new SelectListItem
                        {
                            Value = reader["AreaID"].ToString(),
                            Text = $"{reader["AreaName"]}"
                        };
                        AssessmentArea.Add(area);

                    }
                }
            }
        }//End of 'PopulateAssessmentAreaList'.

        /*--------------------ADMIN PRIV----------------------*/
        private void CheckIfUserIsAdmin(int userId)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = "SELECT SystemUserRole FROM SystemUser WHERE SystemUserID = @SystemUserID";
                SqlCommand cmd = new SqlCommand(cmdText, conn);
                cmd.Parameters.AddWithValue("@SystemUserID", userId);
                conn.Open();
                var result = cmd.ExecuteScalar();

                // If SystemUserRole is 2, set IsUserAdmin to true
                if (Convert.ToInt32(result) == 3)
                {
                    IsAdmin = true;
                    ViewData["IsAdmin"] = true;
                }
                else
                {
                    IsAdmin = false;
                }
            }
        }//End of 'CheckIfUserIsAdmin'.
        /*--------------------ADMIN PRIV----------------------*/
    }// End of 'AddAssessment' Class.
}// End of 'namespace'.
