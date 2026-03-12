using FinancialLiteracyTool.Model.Users;
using FinancialLiteracyTool.MyAppHelper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace FinancialLiteracyTool.Pages.AdminPages.Users
{
    [BindProperties]
    public class EditSystemUsernameModel : PageModel
    {
        public bool IsAdmin { get; set; }
        public ProfileView Profile { get; set; }

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
            PopulateUserName(id);
        }//End of 'OnGet'.

        public IActionResult OnPost(int id)
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {
                    string cmdText = "UPDATE SystemUser SET SystemUsername = @SystemUsername WHERE UserID = @UserID;";
                    SqlCommand cmd = new SqlCommand(cmdText, conn);
                    cmd.Parameters.AddWithValue("@UserID", id);
                    cmd.Parameters.AddWithValue("@SystemUsername", Profile.SystemUserName);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                return RedirectToPage("/Account/Profile");
            }
            else
            {
                OnGet(Profile.SystemUserID);
                return Page();
            }
        }//End of 'OnPost'.

        public void PopulateUserName(int id)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT UserID, SystemUsername FROM SystemUser WHERE UserID = @UserID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UserID", id);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        Profile = new ProfileView
                        {
                            SystemUserID = reader.GetInt32(0),
                            SystemUserName = reader.GetString(1)
                        };
                    }
                }
            }
        }

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
    }// End of 'EditSystemUsername' Class.
}// End of 'namespace'.
