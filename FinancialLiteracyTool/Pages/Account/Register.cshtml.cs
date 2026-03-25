using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FinancialLiteracyTool.Model.LoginRegistration;
using FinancialLiteracyTool.MyAppHelper;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace FinancialLiteracyTool.Pages.Account
{
    public class RegisterModel : PageModel
    {
        [BindProperty]
        public Registration NewUser { get; set; }

        public List<string> PasswordErrors { get; set; } = new();

        public IActionResult OnPost()
        {
            PasswordErrors.Clear();
            var firstName = (NewUser.FirstName ?? string.Empty).Trim();
            var lastName = (NewUser.LastName ?? string.Empty).Trim();
            string password = (NewUser.Password ?? string.Empty).Trim();
            var userName = (NewUser.UserName ?? string.Empty).Trim();
            const int dbMaxName = 50;
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

            if (firstName.Length > dbMaxName)
            {
                ModelState.AddModelError("NewUser.FirstName", "First name must be at most 50 characters.");
            }

            if (firstName.Length > dbMaxName)
            {
                ModelState.AddModelError("NewUser.LastName", "Last name must be at most 50 characters.");
            }

            if (userName.Length > dbMaxName)
            {
                ModelState.AddModelError("NewUser.UserName", "Username must be at most 50 characters.");
            }

            string profileImageURL = "/Images/BlankAvatar.png";

            if (ModelState.IsValid)
            {
                using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {
                    conn.Open();

                    string cmdSystemUserText = "INSERT INTO SystemUser (SystemUserFName, SystemUserLName, SystemUsername, SystemUserPassword, SystemUserEmail, SystemUserProfileImage, SystemUserRole) VALUES (@SystemUserFName, @SystemUserLName, @SystemUsername, @SystemUserPassword, @SystemUserEmail, @ProfileImage, @SystemUserRole);";
                    SqlCommand cmdS = new SqlCommand(cmdSystemUserText, conn);
                    cmdS.Parameters.AddWithValue("@SystemUserFName", NewUser.FirstName);
                    cmdS.Parameters.AddWithValue("@SystemUserLName", NewUser.LastName);
                    cmdS.Parameters.AddWithValue("@SystemUsername", NewUser.UserName);
                    cmdS.Parameters.AddWithValue("@SystemUserPassword", AppHelper.GeneratePasswordHash(NewUser.Password));
                    cmdS.Parameters.AddWithValue("@SystemUserEmail", NewUser.Email);
                    cmdS.Parameters.AddWithValue("@ProfileImage", profileImageURL);
                    cmdS.Parameters.AddWithValue("@SystemUserRole", 1);
                    cmdS.ExecuteNonQuery();

                }
                return RedirectToPage("/Account/Login");
            }
            else
            {
                return Page();
            }
        }//End of 'OnPost'.
    }//End of 'Register' Class. 
}//End of 'namespace'.
