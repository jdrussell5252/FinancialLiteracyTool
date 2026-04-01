using FinancialLiteracyTool.Model.Questions;
using FinancialLiteracyTool.Model.Users;
using FinancialLiteracyTool.MyAppHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace FinancialLiteracyTool.Pages.Account
{
    [Authorize]
    [BindProperties]
    public class EditProfileModel : PageModel
    {
        public bool IsAdmin { get; set; }
        public ProfileView Profiles { get; set; } = new ProfileView();
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

            PopulateUsername(id);
            // string str = Profiles.SystemUserName;
        }// End of 'OnGet'.

        public IActionResult OnPost(int id)
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {
                    string cmdText = "UPDATE SystemUser SET SystemUsername = @SystemUsername WHERE SystemUserID = @SystemUserID;";
                    SqlCommand cmd = new SqlCommand(cmdText, conn);
                    cmd.Parameters.AddWithValue("@SystemUserID", id);
                    cmd.Parameters.AddWithValue("@SystemUsername", Profiles.SystemUserName);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                return RedirectToPage("/Account/Profile");
            }
            else
            {
                OnGet(id);
                return Page();
            }
        }//End of 'OnPost'.

        private void PopulateUsername(int id)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT SystemUserID, SystemUsername FROM SystemUser WHERE SystemUserID = @SystemUserID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@SystemUserID", id);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        Profiles = new ProfileView
                        {
                            SystemUserID = reader.GetInt32(0),
                            SystemUserName = reader.GetString(1)
                        };
                    }
                }
            }
        }//End of 'PopulateUsername'.

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
    }// End of 'EditProfile' Class.
}// End of 'namespace'.
