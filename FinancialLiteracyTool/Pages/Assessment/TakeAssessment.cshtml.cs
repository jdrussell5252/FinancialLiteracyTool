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
        public int UserAssessmentID { get; set; }
        public int CurrentQuestionIndex { get; set; }
        public Dictionary<string, string> SavedAnswers { get; set; } = new();

        public void OnGet(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value);
                CheckIfUserIsAdmin(userId);

                var existing = GetExistingSession(userId, id);

                if (existing != null)
                {
                    UserAssessmentID = existing.Value.UserAssessmentID;
                    CurrentQuestionIndex = existing.Value.CurrentQuestionIndex;
                    SavedAnswers = LoadSavedAnswers(UserAssessmentID);
                }
                else
                {
                    UserAssessmentID = CreateNewSession(userId, id);
                    CurrentQuestionIndex = 0;
                }
            }

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

        private (int UserAssessmentID, int CurrentQuestionIndex)? GetExistingSession(int userId, int assessmentId)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = @"SELECT UserAssessmentID, CurrentQuestionIndex 
                         FROM UserAssessment 
                         WHERE SystemUserID = @UserID 
                         AND AssessmentID = @AssessmentID 
                         AND IsFinished = 0";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@AssessmentID", assessmentId);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return (reader.GetInt32(0), int.Parse(reader.GetString(1)));
                }
            }
            return null;
        }// End of 'GetExistingSession'.

        private int CreateNewSession(int userId, int assessmentId)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = @"INSERT INTO UserAssessment 
                            (AssessmentID, SystemUserID, CoachID, IsFinished, StartTime, LastSavedTime, CurrentQuestionIndex)
                         OUTPUT INSERTED.UserAssessmentID
                         VALUES (@AssessmentID, @UserID, 0, 0, @Now, @Now, '0')";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@AssessmentID", assessmentId);
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@Now", DateTime.Now);
                conn.Open();
                return (int)cmd.ExecuteScalar();
            }
        }// End of 'CreateNewSession'.

        private Dictionary<string, string> LoadSavedAnswers(int userAssessmentId)
        {
            var answers = new Dictionary<string, string>();
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT QuestionID, SelectedAnswer FROM UserAssessmentAnswers WHERE UserAssessmentID = @ID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ID", userAssessmentId);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    answers[reader.GetInt32(0).ToString()] = reader.GetString(1);
                }
            }
            return answers;
        }

        public class SaveSingleAnswerRequest
        {
            public int UserAssessmentID { get; set; }
            public int QuestionID { get; set; }
            public string? SelectedAnswer { get; set; }
            public int CurrentQuestionIndex { get; set; }
        }

        public class SaveAnswersRequest
        {
            public int UserAssessmentID { get; set; }
            public Dictionary<string, string>? Answers { get; set; }
        }

        public IActionResult OnPostSaveSingleAnswer([FromBody] SaveSingleAnswerRequest request)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                conn.Open();

                // INSERT or UPDATE the answer
                string upsert = @"
            IF EXISTS (SELECT 1 FROM UserAssessmentAnswers WHERE UserAssessmentID = @UAID AND QuestionID = @QID)
                UPDATE UserAssessmentAnswers 
                SET SelectedAnswer = @Answer, LastUpdated = @Now
                WHERE UserAssessmentID = @UAID AND QuestionID = @QID
            ELSE
                INSERT INTO UserAssessmentAnswers (UserAssessmentID, QuestionID, SelectedAnswer, LastUpdated)
                VALUES (@UAID, @QID, @Answer, @Now)";

                SqlCommand cmd = new SqlCommand(upsert, conn);
                cmd.Parameters.AddWithValue("@UAID", request.UserAssessmentID);
                cmd.Parameters.AddWithValue("@QID", request.QuestionID);
                cmd.Parameters.AddWithValue("@Answer", request.SelectedAnswer);
                cmd.Parameters.AddWithValue("@Now", DateTime.Now);
                cmd.ExecuteNonQuery();

                // Update progress in UserAssessment
                string update = @"UPDATE UserAssessment 
                          SET CurrentQuestionIndex = @Index, LastSavedTime = @Now 
                          WHERE UserAssessmentID = @UAID";
                SqlCommand cmd2 = new SqlCommand(update, conn);
                cmd2.Parameters.AddWithValue("@Index", request.CurrentQuestionIndex.ToString());
                cmd2.Parameters.AddWithValue("@Now", DateTime.Now);
                cmd2.Parameters.AddWithValue("@UAID", request.UserAssessmentID);
                cmd2.ExecuteNonQuery();
            }
            return new OkResult();
        }

        public IActionResult OnPostSaveAnswers([FromBody] Dictionary<string, string> answers)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                // Mark assessment as finished
                using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {
                    string query = @"UPDATE UserAssessment 
                             SET IsFinished = 1, LastSavedTime = @Now 
                             WHERE UserAssessmentID = @UAID";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Now", DateTime.Now);
                    cmd.Parameters.AddWithValue("@UAID", /* pass UserAssessmentID from request */ 0);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["AnswersJson"] = JsonSerializer.Serialize(answers);
            return new OkResult();
        }

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

    }// End of 'TakeAssessment' Class.
    public class SaveAnswersRequest
    {
        public int UserAssessmentID { get; set; }
        public Dictionary<string, string> Answers { get; set; }
    }
}// End of 'namespace'.
