using FinancialLiteracyTool.Model.Users;
using FinancialLiteracyTool.MyAppHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace FinancialLiteracyTool.Pages.AdminPages.Users
{
    [BindProperties]
    [Authorize]
    public class EditUsersModel : PageModel
    {
        public bool IsAdmin { get; set; }
        //public bool IsCoach { get; set; }
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
            PopulateSystemUserInfo(id);
        }//End of 'OnGet'.

        public IActionResult OnPost(int id)
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {
                    string cmdText = "UPDATE SystemUser SET SystemUserFName = @SystemUserFName, SystemUserLName = @SystemUserLName, SystemUsername = @SystemUsername, SystemUserRole = @SystemUserRole WHERE SystemUserID = @SystemUserID;";
                    SqlCommand cmd = new SqlCommand(cmdText, conn);
                    cmd.Parameters.AddWithValue("@SystemUserFName", Profile.SystemUserFirstName);
                    cmd.Parameters.AddWithValue("@SystemUserLName", Profile.SystemUserLastName);
                    cmd.Parameters.AddWithValue("@SystemUsername", Profile.SystemUserName);
                    cmd.Parameters.AddWithValue("@SystemUserRole", Profile.SystemUserRole);
                    cmd.Parameters.AddWithValue("@SystemUserID", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                return RedirectToPage("/AdminPages/Users/BrowseUsers");
            }
            else
            {
                OnGet(Profile.SystemUserID);
                return Page();
            }
        }//End of 'OnPost'.

        public void PopulateSystemUserInfo(int id)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT SystemUserFName, SystemUserLName, SystemUsername, SystemUserRole FROM SystemUser WHERE SystemUserID = @SystemUserID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@SystemUserID", id);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        Profile = new ProfileView
                        {
                            SystemUserFirstName = reader.GetString(0),
                            SystemUserLastName = reader.GetString(1),
                            SystemUserName = reader.GetString(2),
                            SystemUserRole = reader.GetInt32(3)
                        };

                    }
                }
            }
        }// End of 'PopulateSystemUserInfo'.

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
    }// End of '' Class.
}// End of 'namespace'.
