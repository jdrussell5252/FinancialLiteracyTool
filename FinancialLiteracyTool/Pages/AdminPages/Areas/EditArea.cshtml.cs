using FinancialLiteracyTool.Model.Areas;
using FinancialLiteracyTool.MyAppHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace FinancialLiteracyTool.Pages.AdminPages.Areas
{
    [Authorize]
    [BindProperties]
    public class EditAreaModel : PageModel
    {
        public bool IsAdmin { get; set; }
        public AreaView Areas { get; set; } = new AreaView();
        public IActionResult OnGet(int id)
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

            if (!IsAdmin)
            {
                return Forbid();
            }

            PopulateAreaName(id);

            return Page();
        }// End of 'OnGet'.

        public IActionResult OnPost(int id)
        {
            var areaName = (Areas.AreaName ?? string.Empty).Trim();
            const int areaNameMax = 50;

            if (areaName.Length > areaNameMax)
            {
                ModelState.AddModelError("Areas.AreaName", "Area Name must not exceed 50 characters.");
            }

            if (ModelState.IsValid)
            {
                using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {

                    conn.Open();
                    string insertcmdText = "UPDATE Area SET AreaName = @AreaName WHERE AreaID = @AreaID;";
                    SqlCommand insertcmd = new SqlCommand(insertcmdText, conn);
                    insertcmd.Parameters.AddWithValue("@AreaName", Areas.AreaName);
                    insertcmd.Parameters.AddWithValue("@AreaID", id);
                    insertcmd.ExecuteScalar();

                }
                return RedirectToPage("BrowseAreas");
            }
            return Page();
        }// End of 'OnPost'.

        private void PopulateAreaName(int id)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT AreaID, AreaName FROM Area WHERE AreaID = @AreaID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@AreaID", id);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        Areas = new AreaView
                        {
                            AreaID = reader.GetInt32(0),
                            AreaName = reader.GetString(1)
                        };
                    }
                }
            }
        }//End of 'PopulateAreaName'.

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
    }// End of 'EditArea' Class.
}// End of 'namespace'.
