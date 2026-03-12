using FinancialLiteracyTool.MyAppHelper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace FinancialLiteracyTool.Pages.AdminPages.Questions
{
    [BindProperties]
    public class EditQuestionTypeModel : PageModel
    {
        public bool IsAdmin { get; set; }
        public List<SelectListItem> Questions { get; set; } = new List<SelectListItem>();
        public int SelectedQuestionTypeID { get; set; }
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

            PopulateCurrentQuestionType(id);
            PopulateQuestionTypeList();
        }// End of 'OnGet'.

        public IActionResult OnPost(int id)
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {
                    string cmdText = "UPDATE Question SET QuestionTypeID = @QuestionTypeID WHERE QuestionID = @QuestionID;";
                    SqlCommand cmd = new SqlCommand(cmdText, conn);
                    cmd.Parameters.AddWithValue("@QuestionTypeID", SelectedQuestionTypeID);
                    cmd.Parameters.AddWithValue("@QuestionID", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                return RedirectToPage("BrowseQuestions");
            }
            else
            {
                OnGet(id);
                return Page();
            }
        }//End of 'OnPost'.

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
                        Questions.Add(question);

                    }
                }
            }
        }//End of 'PopulateQuestionTypeList'.

        /*--------------------ADMIN PRIV----------------------*/
        private void CheckIfUserIsAdmin(int userId)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = "SELECT IsAdmin FROM SystemUser WHERE SystemUserID = @SystemUserID";
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
    }// End of '' Class.
}// End of 'namespace'.
