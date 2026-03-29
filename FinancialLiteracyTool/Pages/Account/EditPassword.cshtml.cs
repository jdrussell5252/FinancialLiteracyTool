using FinancialLiteracyTool.Model.LoginRegistration;
using FinancialLiteracyTool.Model.Users;
using FinancialLiteracyTool.MyAppHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace FinancialLiteracyTool.Pages.Account
{
    [Authorize]
    public class EditPasswordModel : PageModel
    {
        [BindProperty]
        public Password Profile { get; set; }
        public List<string> PasswordErrors { get; set; } = new();
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
        }//End of 'OnGet'.

        public IActionResult OnPost(int id)
        {
            PasswordErrors.Clear();
            string password = (Profile.MyPassword ?? string.Empty).Trim();
            const int dbMaxPassword = 20;


            if (password.Length < 10)
                PasswordErrors.Add("Password must be at least 10 characters long.");
            if (!Regex.IsMatch(password, @"\d"))
                PasswordErrors.Add("Password must contain at least one number.");
            if (!Regex.IsMatch(password, @"[A-Z]"))
                PasswordErrors.Add("Password must contain at least one uppercase letter.");
            if (!Regex.IsMatch(password, @"[a-z]"))
                PasswordErrors.Add("Password must contain at least one lowercase letter.");

            if (password.Length > dbMaxPassword)
            {
                ModelState.AddModelError("NewUser.Password", "Password must be at most 50 characters.");
            }

            if (ModelState.IsValid)
            {
                using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {
                    conn.Open();

                    string cmdSystemUserText = "UPDATE SystemUser SET SystemUserPassword = @SystemUserPassword WHERE SystemUserID = @SystemUserID;";
                    SqlCommand cmdS = new SqlCommand(cmdSystemUserText, conn);
                    cmdS.Parameters.AddWithValue("@SystemUserID", id);
                    cmdS.Parameters.AddWithValue("@SystemUserPassword", AppHelper.GeneratePasswordHash(Profile.MyPassword));
                    cmdS.ExecuteNonQuery();

                }
                return RedirectToPage("/Account/Profile");
            }
            else
            {
                return Page();
            }
        }//End of 'OnPost'.

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
    }// End of 'EditPassword'.
}// End of 'namespace'.
