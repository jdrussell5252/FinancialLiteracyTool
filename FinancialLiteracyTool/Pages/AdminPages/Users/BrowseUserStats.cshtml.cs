using FinancialLiteracyTool.MyAppHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace FinancialLiteracyTool.Pages.AdminPages
{
    [Authorize]
    public class BrowseUserStatsModel : PageModel
    {
        public bool IsAdmin { get; set; }
        public int TotalUsers { get; set; }
        public int AssessmentsTaken { get; set; }
        public int Completed { get; set; }
        public int InProgress { get; set; }
        public double CompletionRate { get; set; }
        public double AverageScore { get; set; }

        public IActionResult OnGet()
        {
            // 🔹 Get logged-in user ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value);
                CheckIfUserIsAdmin(userId);
            }

            // 🔹 Block non-admins
            if (!IsAdmin)
            {
                return Forbid();
            }

            // 🔹 Load stats
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                conn.Open();

                TotalUsers = Convert.ToInt32(new SqlCommand(
                    "SELECT COUNT(*) FROM dbo.SystemUser", conn).ExecuteScalar() ?? 0);

                AssessmentsTaken = Convert.ToInt32(new SqlCommand(
                    "SELECT COUNT(*) FROM dbo.UserAssessments", conn).ExecuteScalar() ?? 0);

                Completed = Convert.ToInt32(new SqlCommand(
                    "SELECT COUNT(*) FROM dbo.UserAssessments WHERE IsFinished = 1", conn).ExecuteScalar() ?? 0);

                InProgress = Convert.ToInt32(new SqlCommand(
                    "SELECT COUNT(*) FROM dbo.UserAssessments WHERE IsFinished = 0", conn).ExecuteScalar() ?? 0);

                CompletionRate = Convert.ToDouble(new SqlCommand(@"
                    SELECT CASE 
                        WHEN COUNT(*) = 0 THEN 0
                        ELSE COUNT(CASE WHEN IsFinished = 1 THEN 1 END) * 100.0 / COUNT(*) 
                    END
                    FROM dbo.UserAssessments", conn).ExecuteScalar() ?? 0);

                AverageScore = Convert.ToDouble(new SqlCommand(
                    "SELECT AVG(CAST(Result AS FLOAT)) FROM dbo.Results", conn).ExecuteScalar() ?? 0);
            }

            return Page();
        }

        private void CheckIfUserIsAdmin(int userId)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = "SELECT SystemUserRole FROM SystemUser WHERE SystemUserID = @SystemUserID";
                SqlCommand cmd = new SqlCommand(cmdText, conn);
                cmd.Parameters.AddWithValue("@SystemUserID", userId);

                conn.Open();
                var result = cmd.ExecuteScalar();

                if (result != null && Convert.ToInt32(result) == 3)
                {
                    IsAdmin = true;
                    ViewData["IsAdmin"] = true;
                }
                else
                {
                    IsAdmin = false;
                }
            }
        }
    }
}