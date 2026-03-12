using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinancialLiteracyTool.Pages.Account
{
    public class SignOutModel : PageModel
    {
        public void OnGet()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Redirect("/");
        }// End of 'OnGet'.
    }// End of 'SignOut' Class.
}// End of 'namespace'.
