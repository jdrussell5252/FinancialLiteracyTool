using FinancialLiteracyTool.Model.Users;
using FinancialLiteracyTool.MyAppHelper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace FinancialLiteracyTool.Pages.AdminPages.Users
{
    public class EditUserIsAdminModel : PageModel
    {
        [BindProperty]
        public SystemUserView Users { get; set; } = new SystemUserView();
        public bool IsAdmin { get; set; }
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
            PopulateUserList(id);
        }//End of 'OnGet'.

        public IActionResult OnPost(int id)
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {
                    string cmdText = "UPDATE SystemUser SET IsAdmin = @IsAdmin WHERE UserID = @UserID";
                    SqlCommand cmd = new SqlCommand(cmdText, conn);
                    cmd.Parameters.AddWithValue("@IsAdmin", Users.IsAdmin);
                    cmd.Parameters.AddWithValue("@UserID", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                return RedirectToPage("BrowseUsers");
            }
            return Page();
        }//End of 'OnPost'.

        private void PopulateUserList(int id)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT UserID, IsAdmin FROM SystemUser WHERE UserID = @UserID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UserID", id);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        Users = new SystemUserView
                        {
                            UserID = reader.GetInt32(0),
                            IsAdmin = reader.GetBoolean(1)
                        };
                    }
                }
            }
        }//End of 'PopulateEmployeeList'.

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
    }// End of 'EditUserIsAdmin' Class.
}// End of 'namespace'.
