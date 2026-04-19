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
    public class ProfileModel : PageModel
    {
        public bool IsAdmin { get; set; }
        public ProfileView CurrentProfile { get; set; } = new ProfileView();

        public int AssessmentsTaken { get; set; }

        public void OnGet()
        {
      

            // Safely access the NameIdentifier claim
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            /*--------------------ADMIN PRIV----------------------*/
            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value); // Use the claim value only if it exists
                CheckIfUserIsAdmin(userId);
                PopulateProfileImage(userId);
                GetAssessmentsTaken(userId);
            }
            /*--------------------ADMIN PRIV----------------------*/
        }//End of 'OnGet'.

        private void PopulateProfileImage(int id)
        {

            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = "SELECT SystemUserID, SystemUserProfileImage, SystemUsername, SystemUserFName, SystemUserLName FROM SystemUser " +
                    "WHERE SystemUserID = @SystemUserID";
                SqlCommand cmd = new SqlCommand(cmdText, conn);
                cmd.Parameters.AddWithValue("@SystemUserID", id);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    reader.Read();

                    // ID + Image
                    if (reader[0] != System.DBNull.Value)
                    {
                        CurrentProfile.SystemUserID = reader.GetInt32(0);
                    }

                    if (reader[1] != System.DBNull.Value)
                    {
                        CurrentProfile.SystemUserProfileImagePath = reader.GetString(1);
                    }
                    else
                    {
                        CurrentProfile.SystemUserProfileImagePath = "";
                    }

                    // Username
                    if (reader[2] != System.DBNull.Value)
                    {
                        CurrentProfile.SystemUserName = reader.GetString(2);
                    }
                    else
                    {
                        CurrentProfile.SystemUserName = "";
                    }

                    // First Name
                    if (reader[3] != System.DBNull.Value)
                    {
                        CurrentProfile.SystemUserFirstName = reader.GetString(3);
                    }
                    else
                    {
                        CurrentProfile.SystemUserFirstName = "";
                    }

                    // Last Name
                    if (reader[4] != System.DBNull.Value)
                    {
                        CurrentProfile.SystemUserLastName = reader.GetString(4);
                    }
                    else
                    {
                        CurrentProfile.SystemUserLastName = "";
                    }
                }
            }



            
        }//End of 'PopulateProfileImage'.

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

        private void GetAssessmentsTaken(int userId)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = "SELECT COUNT(*) FROM UserAssessments WHERE SystemUserID = @SystemUserID";
                SqlCommand cmd = new SqlCommand(cmdText, conn);
                cmd.Parameters.AddWithValue("@SystemUserID", userId);
                conn.Open();
                var result = cmd.ExecuteScalar();
                AssessmentsTaken = Convert.ToInt32(result);
            }
        }
        /*--------------------ADMIN PRIV----------------------*/
    }// End of 'Profile' Class.
}// End of 'namespace'.
