using FinancialLiteracyTool.Model.Users;
using FinancialLiteracyTool.MyAppHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace FinancialLiteracyTool.Pages.CoachPages
{
    [Authorize]
    public class BrowseCoachedUsersModel : PageModel
    {
        public bool IsCoach { get; set; }
        public List<SystemUserView> Users { get; set; } = new List<SystemUserView>();
        public IActionResult OnGet()
        {

            // Safely access the NameIdentifier claim
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            /*--------------------ADMIN PRIV----------------------*/
            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value); // Use the claim value only if it exists
                CheckIfUserIsCoach(userId);
                PopulateUserList(userId);
            }
            /*--------------------ADMIN PRIV----------------------*/

            if (!IsCoach)
            {
                return Forbid();
            }

            return Page();
        }//End of 'OnGet'.

        private void PopulateUserList(int id)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = @"
                SELECT 
                    su.SystemUserID,
                    su.SystemUserFname,
                    su.SystemUserLName,
                    su.SystemUsername
                FROM CoachedUsers cu
                INNER JOIN SystemUser su 
                    ON cu.SystemUserID = su.SystemUserID
                WHERE cu.CoachID = @CoachID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@CoachID", id);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        SystemUserView Auser = new SystemUserView
                        {
                            UserID = reader.GetInt32(0),
                            UserFName = reader.GetString(1),
                            UserLName = reader.GetString(2),
                            SystemUsername = reader.GetString(3),
                        };
                        Users.Add(Auser);

                    }
                }
            }
        }//End of 'PopulateUserList'.

        /*--------------------COACH PRIV----------------------*/
        private void CheckIfUserIsCoach(int userId)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = "SELECT SystemUserRole FROM SystemUser WHERE SystemUserID = @SystemUserID";
                SqlCommand cmd = new SqlCommand(cmdText, conn);
                cmd.Parameters.AddWithValue("@SystemUserID", userId);
                conn.Open();
                var result = cmd.ExecuteScalar();

                // If SystemUserRole is 2, set IsUserAdmin to true
                if (result != null && result.ToString() == "2")
                {
                    IsCoach = true;
                    ViewData["IsCoach"] = true;
                }
                else
                {
                    IsCoach = false;
                }
            }
        }//End of 'CheckIfUserIsCoach'.
        /*--------------------END OF COACH PRIV----------------------*/

    }// End of '' Class.
}// End of 'namespace'.
