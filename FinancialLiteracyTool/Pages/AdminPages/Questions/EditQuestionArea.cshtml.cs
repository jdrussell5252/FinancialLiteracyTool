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
    public class EditQuestionAreaModel : PageModel
    {
        public bool IsAdmin { get; set; }
        public List<SelectListItem> Questions { get; set; } = new List<SelectListItem>();
        public int SelectedQuestionAreaID { get; set; }
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

            PopulateCurrentQuestionArea(id);
            PopulateQuestionAreaList();
        }// End of 'OnGet'.

        public IActionResult OnPost(int id)
        { 
            if (ModelState.IsValid)
            {
                using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {
                    string cmdText = "UPDATE Question SET AreaID = @AreaID WHERE QuestionID = @QuestionID;";
                    SqlCommand cmd = new SqlCommand(cmdText, conn);
                    cmd.Parameters.AddWithValue("@AreaID", SelectedQuestionAreaID);
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
                        Questions.Add(question);

                    }
                }
            }
        }//End of 'PopulateQuestionAreaList'.

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
    }// End of 'EditQuestionArea' Class.
}// End of 'namespace'.
