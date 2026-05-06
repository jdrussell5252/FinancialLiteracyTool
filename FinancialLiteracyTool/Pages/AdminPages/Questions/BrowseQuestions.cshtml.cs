using FinancialLiteracyTool.Model.Questions;
using FinancialLiteracyTool.MyAppHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace FinancialLiteracyTool.Pages.AdminPages.Questions
{
    [Authorize]
    public class BrowseQuestionsModel : PageModel
    {
        public List<Question> Questions { get; set; } = new();
        public bool IsAdmin { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 6;
        public int TotalCount { get; set; }
        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize)));

        public IActionResult OnGet(int id, int pageNumber = 1, int pageSize = 6)
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

            if (!IsAdmin)
            {
                return Forbid();
            }

            PopulateQuestions();

            // === Pagination logic ===
            PageNumber = pageNumber < 1 ? 1 : pageNumber;
            PageSize = pageSize < 1 ? 6 : pageSize;

            TotalCount = Questions.Count;

            // Clamp PageNumber so it’s not past the last page
            if (TotalCount > 0 && (PageNumber - 1) * PageSize >= TotalCount)
            {
                PageNumber = (int)Math.Ceiling((double)TotalCount / PageSize);
            }

            if (TotalCount > 0)
            {
                int skip = (PageNumber - 1) * PageSize;
                Questions = Questions
                    .Skip(skip)
                    .Take(PageSize)
                    .ToList();
            }

            return Page();
        }//End of 'OnGet'.

        public IActionResult OnPostDelete(int id)
        {
            // delete the question from the database
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                conn.Open();
                string deleteCmdText = "DELETE FROM QuestionChoices WHERE QuestionID = @QuestionID";
                SqlCommand deleteCmd = new SqlCommand(deleteCmdText, conn);
                deleteCmd.Parameters.AddWithValue("@QuestionID", id);
                deleteCmd.ExecuteNonQuery();

                string deleteCmdText2 = "DELETE FROM Question WHERE QuestionID = @QuestionID";
                SqlCommand deleteCmd2 = new SqlCommand(deleteCmdText2, conn);
                deleteCmd2.Parameters.AddWithValue("@QuestionID", id);
                deleteCmd2.ExecuteNonQuery();
            }

            return RedirectToPage();
        }//End of 'OnPostDelete'.

        /*private void PopulateQuestions()
        {

            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query =
                    "SELECT q.QuestionID, q.QuestionText, qa.QuestionArea, qt.TypeName, " +
                    "qc.ChoiceID, qc.QuestionChoiceText " +
                    "FROM Question AS q " +
                    "INNER JOIN QuestionArea AS qa ON qa.QuestionAreaID = q.QuestionAreaID " +
                    "INNER JOIN QuestionType AS qt ON qt.QuestionTypeID = q.QuestionTypeID " +
                    "INNER JOIN QuestionChoices AS qc ON qc.QuestionID = q.QuestionID " +
                    "ORDER BY q.QuestionID, qc.ChoiceID";

                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();

                using SqlDataReader reader = cmd.ExecuteReader();

                Question currentQuestion;

                while (reader.Read())
                {
                    int questionId = reader.GetInt32(0);

                    currentQuestion = new Question
                    {
                        QuestionID = questionId,
                        QuestionText = reader.GetString(1),
                        QuestionArea = reader.GetString(2),
                        QuestionType = reader.GetString(3),
                        Choices = new List<QuestionChoices>()
                    };

                    Questions.Add(currentQuestion);
              

                    // Add the choice for this row
                    currentQuestion.Choices.Add(new QuestionChoices
                    {
                        QuestionChoiceID = reader.GetInt32(4),
                        QuestionID = questionId,
                        QuestionChoiceText = reader.GetString(5)
                    });
                }
            }
        }*/

        private void PopulateQuestions()
        {
            Questions = new List<Question>();

            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {

                string query =
                  "SELECT q.QuestionID, q.QuestionText, qa.AreaName, qt.TypeName, " +
                  "qc.ChoiceID, qc.QuestionChoiceText, qc.IsCorrect " + 
                  "FROM Question AS q " +
                  "INNER JOIN Area AS qa ON qa.AreaID = q.AreaID " +
                  "INNER JOIN QuestionType AS qt ON qt.QuestionTypeID = q.QuestionTypeID " +
                  "INNER JOIN QuestionChoices AS qc ON qc.QuestionID = q.QuestionID " +
                  "ORDER BY q.QuestionID";

                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();

                using SqlDataReader reader = cmd.ExecuteReader();

                Question currentQuestion = null;
                int currentQuestionId = -1;

                while (reader.Read())
                {
                    int questionId = reader.GetInt32(0);

                    // New question group
                    if (currentQuestion == null || questionId != currentQuestionId)
                    {
                        currentQuestionId = questionId;

                        currentQuestion = new Question
                        {
                            QuestionID = questionId,
                            QuestionText = reader.GetString(1),
                            QuestionArea = reader.GetString(2),
                            QuestionType = reader.GetString(3),
                            Choices = new List<QuestionChoices>()
                        };

                        Questions.Add(currentQuestion);
                    }
                    // Add the choice for this row
                    currentQuestion.Choices.Add(new QuestionChoices
                    {
                        QuestionChoiceID = reader.GetInt32(4),
                        QuestionID = questionId,
                        QuestionChoiceText = reader.GetString(5),
                        IsCorrect = reader.GetBoolean(6) // ✅ FIXED
                    });
                }
            }
        }// End of 'PopulateQuestions'.

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
    }// End of 'BrowseQuestions' Class.
}// End of 'namespace'.
