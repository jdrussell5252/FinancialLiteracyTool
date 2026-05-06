using FinancialLiteracyTool.Model.Questions;
using FinancialLiteracyTool.MyAppHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace FinancialLiteracyTool.Pages.AdminPages.Questions
{
    [Authorize]
    [BindProperties]
    public class EditQuestionModel : PageModel
    {
        public bool IsAdmin { get; set; }
        public QuestionView Questions { get; set; } = new QuestionView();
        public List<SelectListItem> QuestionArea { get; set; } = new List<SelectListItem>();
        public int SelectedQuestionAreaID { get; set; }
        public List<SelectListItem> TypeOfQuestions { get; set; } = new List<SelectListItem>();
        public int SelectedQuestionTypeID { get; set; }
        public List<string> Choices { get; set; } = new();
        public int? CorrectChoiceIndex { get; set; }

        public IActionResult OnGet(int id)
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

            PopulateCurrentQuestionArea(id);
            PopulateQuestionAreaList();
            PopulateQuestionText(id);
            PopulateCurrentQuestionType(id);
            PopulateQuestionTypeList();
            PopulateQuestionChoices(id);

            return Page();
        }// End of 'OnGet'.

        public IActionResult OnPost(int id)
        {
            var QuestionText = (Questions.QuestionText ?? string.Empty).Trim();
            const int dbMaxQuestionText = 255;

            if (QuestionText.Length > dbMaxQuestionText)
            {
                ModelState.AddModelError("Questions.QuestionText", "Question Text must not exceed 255 words.");
            }
            for (int i = 0; i < Choices.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(Choices[i]))
                {
                    ModelState.AddModelError($"Choices[{i}]", "Choice text cannot be empty.");
                }
            }
            if (ModelState.IsValid)
            {
                using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {
                    conn.Open();

                    // Update main question
                    string cmdText = @"
                    UPDATE Question
                    SET QuestionText = @QuestionText,
                    AreaID = @AreaID,
                    QuestionTypeID = @QuestionTypeID
                    WHERE QuestionID = @QuestionID;";

                    SqlCommand cmd = new SqlCommand(cmdText, conn);
                    cmd.Parameters.AddWithValue("@QuestionText", Questions.QuestionText);
                    cmd.Parameters.AddWithValue("@AreaID", SelectedQuestionAreaID);
                    cmd.Parameters.AddWithValue("@QuestionTypeID", SelectedQuestionTypeID);
                    cmd.Parameters.AddWithValue("@QuestionID", id);
                    cmd.ExecuteNonQuery();

                    // Load existing choice ids
                    string selectChoicesSql = @"
                    SELECT ChoiceID
                    FROM QuestionChoices
                    WHERE QuestionID = @QuestionID
                    ORDER BY ChoiceID;";

                    SqlCommand selectCmd = new SqlCommand(selectChoicesSql, conn);
                    selectCmd.Parameters.AddWithValue("@QuestionID", id);

                    var choiceIds = new List<int>();
                    using (var reader = selectCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            choiceIds.Add(reader.GetInt32(0));
                        }
                    }

                    // Update each choice
                    for (int i = 0; i < choiceIds.Count && i < Choices.Count; i++)
                    {
                        string updateChoiceSql = @"
                        UPDATE QuestionChoices
                        SET QuestionChoiceText = @QuestionChoiceText,
                        IsCorrect = @IsCorrect
                        WHERE ChoiceID = @ChoiceID;";

                        SqlCommand updateChoiceCmd = new SqlCommand(updateChoiceSql, conn);
                        updateChoiceCmd.Parameters.AddWithValue("@QuestionChoiceText", Choices[i] ?? "");
                        updateChoiceCmd.Parameters.AddWithValue("@IsCorrect", CorrectChoiceIndex.HasValue && CorrectChoiceIndex.Value == i);
                        updateChoiceCmd.Parameters.AddWithValue("@ChoiceID", choiceIds[i]);

                        updateChoiceCmd.ExecuteNonQuery();
                    }
                }
                return RedirectToPage("BrowseQuestions");
            }
            else
            {
                OnGet(id);
                return Page();
            }
        }//End of 'OnPost'.

        private void PopulateQuestionText(int id)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT QuestionID, QuestionText FROM Question WHERE QuestionID = @QuestionID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@QuestionID", id);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        Questions = new QuestionView
                        {
                            QuestionID = reader.GetInt32(0),
                            QuestionText = reader.GetString(1)
                        };
                    }
                }
            }
        }//End of 'PopulateQuestionText'.

        private void PopulateCurrentQuestionArea(int id)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string sql = "SELECT AreaID FROM Question WHERE QuestionID = @QuestionID";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@QuestionID", id);

                conn.Open();
                var result = cmd.ExecuteScalar();

                if (result != null)
                    SelectedQuestionAreaID = Convert.ToInt32(result);
            }
        }//End of 'PopulateCurrentQuestionArea'.

        private void PopulateQuestionAreaList()
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
                        var question = new SelectListItem
                        {
                            Value = reader["AreaID"].ToString(),
                            Text = $"{reader["AreaName"]}"
                        };
                        QuestionArea.Add(question);

                    }
                }
            }
        }//End of 'PopulateQuestionAreaList'.

        private void PopulateCurrentQuestionType(int id)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string sql = "SELECT QuestionTypeID FROM Question WHERE QuestionID = @QuestionID";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@QuestionID", id);

                conn.Open();
                var result = cmd.ExecuteScalar();

                if (result != null)
                    SelectedQuestionTypeID = Convert.ToInt32(result);
            }
        }//End of 'PopulateCurrentQuestionType'.

        private void PopulateQuestionTypeList()
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT * FROM QuestionType";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var question = new SelectListItem
                        {
                            Value = reader["QuestionTypeID"].ToString(),
                            Text = $"{reader["TypeName"]}"
                        };
                        TypeOfQuestions.Add(question);

                    }
                }
            }
        }//End of 'PopulateQuestionTypeList'.

        private void PopulateQuestionChoices(int questionId)
        {
            Choices = new List<string>();

            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = @"
            SELECT QuestionChoiceText, IsCorrect
            FROM QuestionChoices
            WHERE QuestionID = @QuestionID
            ORDER BY ChoiceID";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@QuestionID", questionId);

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                int index = 0;
                while (reader.Read())
                {
                    Choices.Add(reader["QuestionChoiceText"]?.ToString() ?? "");

                    if (reader["IsCorrect"] != DBNull.Value && Convert.ToBoolean(reader["IsCorrect"]) == true)
                    {
                        CorrectChoiceIndex = index;
                    }

                    index++;
                }
            }

            while (Choices.Count < 4)
            {
                Choices.Add(string.Empty);
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
    }// End of 'EditQuestion' Class.
}// End of 'namespace'.
