using FinancialLiteracyTool.Model.Assessments;
using FinancialLiteracyTool.Model.Questions;
using FinancialLiteracyTool.MyAppHelper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using System.Text.Json;

namespace FinancialLiteracyTool.Pages.Assessment
{
    // This is the old take assessment page atm, the functional one is in /UserPages/TakeAssessment
    // Will ask later about how this should be organized
    public class TakeAssessmentModel : PageModel
    {
        public bool IsAdmin { get; set; }
        public List<Question> Questions { get; set; } = new();
        public List<QuestionChoices> Choices { get; set; } = new();
        public MyAssessment ThisAssessment { get; set; } = new();

        public void OnGet(int id)
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
            
            PopulateQuestions();
            PopulateQuestionChoices();
        }//End of 'OnGet'.


        /*
         * Questions should be sorted by area.
         */
        private void PopulateQuestions()
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT QuestionID, QuestionText FROM Question";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        Questions.Add(new Question
                        {
                            QuestionID = reader.GetInt32(0),
                            QuestionText = reader.GetString(1)
                        });
                    }
                }
            }
        }//End of 'PopulateQuestions'.

        private void PopulateQuestionChoices()
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT q.QuestionID, qc.QuestionChoiceText FROM Question AS q JOIN QuestionChoices AS qc ON q.QuestionID = qc.QuestionID";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        Choices.Add(new QuestionChoices
                        {
                            QuestionID = reader.GetInt32(0),
                            QuestionChoiceText = reader.GetString(1)
                        });
                    }
                }
            }
        }// End of 'PopulateQuestionChoices'.

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

                // If SystemUserRole is 1, set IsUserAdmin to true
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
        }//End of 'CheckIfUserIsAdmin'.
        /*--------------------ADMIN PRIV----------------------*/

        public IActionResult OnPostSaveAnswers([FromBody] Dictionary<string, string> answers)
        {
            // Store JSON for ConfirmSubmission to read
            TempData["AnswersJson"] = JsonSerializer.Serialize(answers);
            return new OkResult();
        }
    }// End of 'TakeAssessment' Class.

}// End of 'namespace'.
