using FinancialLiteracyTool.MyAppHelper;
using FinancialLiteracyTool.Model.Assessments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using FinancialLiteracyTool.Model.Users;

namespace FinancialLiteracyTool.Pages.CoachPages
{
    [Authorize]
    [BindProperties]
    public class AssignAssessmentModel : PageModel
    {
        public bool IsCoach { get; set; }
        public int SelectedUserID { get; set; }
        public string SelectedUserName { get; set; } = "";

        public int SelectedAssessmentID { get; set; }
        public List<SelectListItem> Assessments { get; set; } = new();
        public IActionResult OnGet(int id)
        {
            // Safely access the NameIdentifier claim
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            /*--------------------ADMIN PRIV----------------------*/
            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value); // Use the claim value only if it exists
                CheckIfUserIsCoach(userId);
            }
            /*--------------------ADMIN PRIV----------------------*/

            PopulateSelectedUser(id);
            PopulateAssessments();

            if (!IsCoach)
            {
                return Forbid();
            }

            return Page();
        }//End of 'OnGet'.

        public IActionResult OnPost()
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                /*--------------------ADMIN PRIV----------------------*/
                if (userIdClaim != null)
                {
                    int userId = int.Parse(userIdClaim.Value); // Use the claim value only if it exists
                    CheckIfUserIsCoach(userId);


                    conn.Open();
                    string insertcmdText = "INSERT INTO UserAssessments (AssessmentID, SystemUserID, CoachID, IsFinished) VALUES (@AssessmentID, @SystemUserID, @CoachID, @IsFinished);";
                    SqlCommand insertcmd = new SqlCommand(insertcmdText, conn);
                    insertcmd.Parameters.AddWithValue("@AssessmentID", SelectedAssessmentID);
                    insertcmd.Parameters.AddWithValue("@SystemUserID", SelectedUserID);
                    insertcmd.Parameters.AddWithValue("@CoachID", userId);
                    insertcmd.Parameters.AddWithValue("@IsFinished", false);
                    insertcmd.ExecuteScalar();
                }

            }
            return RedirectToPage("BrowseCoachedUsers");
        }

        private void PopulateSelectedUser(int userId)
        {
            using SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString());
            string query = @"SELECT SystemUserID
                             FROM SystemUser
                             WHERE SystemUserID = @SystemUserID";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@SystemUserID", userId);

            conn.Open();
            SqlDataReader reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                SelectedUserID = userId;
            }
        }

        private void PopulateAssessments()
        {

            using SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString());
            string query = @"SELECT AssessmentID, AssessmentName
                             FROM Assessment
                             ORDER BY AssessmentName";

            SqlCommand cmd = new SqlCommand(query, conn);
            conn.Open();

            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Assessments.Add(new SelectListItem
                {
                    Value = reader["AssessmentID"].ToString(),
                    Text = reader["AssessmentName"].ToString()
                });
            }
        }

        private void AssignAssessmentToUser(int systemUserId, int assessmentId, int coachId)
        {
            using SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString());

            string query = @"INSERT INTO UserAssessments
                             (SystemUserID, AssessmentID, CoachID)
                             VALUES
                             (@SystemUserID, @AssessmentID, @CoachID)";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@SystemUserID", systemUserId);
            cmd.Parameters.AddWithValue("@AssessmentID", assessmentId);
            cmd.Parameters.AddWithValue("@CoachID", coachId);

            conn.Open();
            cmd.ExecuteNonQuery();
        }

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
    }// End of 'AssignAssessment' Class.
}// End of 'namespace'.
