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
    public class AddQuestionModel : PageModel
    {
        public bool IsAdmin { get; set; }
        public MyQuestions NewQuestion { get; set; } = new MyQuestions();
        public List<string> Choices { get; set; } = new();
        public int? CorrectChoiceIndex { get; set; }
        public bool? TrueFalseCorrect { get; set; }

        public List<SelectListItem> QuestionArea { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> QuestionType { get; set; } = new List<SelectListItem>();
        public int SelectedQuestionTypeID { get; set; }
        public int SelectedQuestionAreaID { get; set; }

        public void OnGet()
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

            PopulateQuestionAreaList();
            PopulateQuestionTypeList();
        }//End of 'OnGet'.

        public IActionResult OnPost(int id)
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {

                    conn.Open();
                    string insertcmdText = "INSERT INTO Question (QuestionText, AreaID, QuestionTypeID) VALUES (@QuestionText, @AreaID, @QuestionTypeID);";
                    SqlCommand insertcmd = new SqlCommand(insertcmdText, conn);
                    insertcmd.Parameters.AddWithValue("@QuestionText", NewQuestion.QuestionText);
                    insertcmd.Parameters.AddWithValue("@AreaID", SelectedQuestionAreaID);
                    insertcmd.Parameters.AddWithValue("@QuestionTypeID", SelectedQuestionTypeID);
                    insertcmd.ExecuteNonQuery();
                    int newQuestionId;
                    // fetch the generated AutoNumber (RecapID)
                    using (var idCmd = new SqlCommand("SELECT @@IDENTITY;", conn))
                    {
                        newQuestionId = Convert.ToInt32(idCmd.ExecuteScalar());
                        NewQuestion.QuestionID = newQuestionId;
                    }

                    /*if (multiplechoice)
                    {
                        string insertcmdText2 =
                             "INSERT INTO QuestionChoices (QuestionID, QuestionChoiceText, IsCorrect) VALUES (@QuestionID, @QuestionChoiceText, @IsCorrect);";
                          foreach (int questionId in questionchoices) {
                                using SqlCommand insertcmd4 = new SqlCommand(insertcmdText4, conn);
                                insertcmd2.Parameters.AddWithValue("@QuestionID", questionId);
                                insertcmd2.Parameters.AddWithValue("@QuestionChoiceText", questionId);
                                insertcmd2.Parameters.AddWithValue("@IsCorrect", );
                                insertcmd2.ExecuteNonQuery();

                          }
                    }*/

                    /*if (true/false)
                    {
                        string insertcmdText3 =
                             "INSERT INTO QuestionChoices (QuestionID, QuestionChoiceText, IsCorrect) VALUES (@QuestionID, @QuestionChoiceText, @IsCorrect);";
                          foreach (int questionId in questionchoices) {
                                using SqlCommand insertcmd4 = new SqlCommand(insertcmdText4, conn);
                                insertcmd3.Parameters.AddWithValue("@QuestionID", questionId);
                                insertcmd3.Parameters.AddWithValue("@QuestionChoiceText", questionId);
                                insertcmd3.Parameters.AddWithValue("@IsCorrect", );
                                insertcmd3.ExecuteNonQuery();

                          }
                    }*/

                    // 2) Decide how to insert choices
                    string typeName = GetQuestionTypeName(conn, SelectedQuestionTypeID).ToLowerInvariant();

                    if (typeName.Contains("multiple"))
                    {
                        if (CorrectChoiceIndex == null || CorrectChoiceIndex < 0 || CorrectChoiceIndex > 3)
                        {
                            ModelState.AddModelError("", "Pick which choice is correct.");
                            return Page();
                        }

                        // insert all 4 (or only non-empty — your choice)
                        for (int i = 0; i < Choices.Count; i++)
                        {
                            string text = (Choices[i] ?? "").Trim();
                            if (string.IsNullOrWhiteSpace(text)) continue;

                            InsertChoice(conn, newQuestionId, text, i == CorrectChoiceIndex.Value);
                        }
                    }
                    else if (typeName.Contains("true") || typeName.Contains("false"))
                    {
                        if (TrueFalseCorrect == null)
                        {
                            ModelState.AddModelError("", "Select whether True or False is correct.");
                            return Page();
                        }

                        InsertChoice(conn, newQuestionId, "True", TrueFalseCorrect.Value == true);
                        InsertChoice(conn, newQuestionId, "False", TrueFalseCorrect.Value == false);
                    }
                }
                return RedirectToPage("BrowseQuestions");
            }
            else
            {
                OnGet();
                // If the model state is not valid, return to the same page with validation errors
                return Page();
            }
        }// End of 'OnPost'.

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
                        QuestionType.Add(question);
                    }
                }
            }
        }//End of 'PopulateQuestionTypeList'.

        private string GetQuestionTypeName(SqlConnection conn, int typeId)
        {
            using var cmd = new SqlCommand("SELECT TypeName FROM QuestionType WHERE QuestionTypeID = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", typeId);
            return (cmd.ExecuteScalar()?.ToString() ?? "").Trim();
        }

        private void InsertChoice(SqlConnection conn, int questionId, string text, bool isCorrect)
        {
            string sql = @"
        INSERT INTO QuestionChoices (QuestionID, QuestionChoiceText, IsCorrect)
        VALUES (@QuestionID, @Text, @IsCorrect);";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@QuestionID", questionId);
            cmd.Parameters.AddWithValue("@Text", text);
            cmd.Parameters.AddWithValue("@IsCorrect", isCorrect);
            cmd.ExecuteNonQuery();
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
    }// End of 'AddQuestion' Class.
}// End of 'namespace'.
