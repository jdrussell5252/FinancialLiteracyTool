using FinancialLiteracyTool.Model.Assessments;
using FinancialLiteracyTool.MyAppHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace FinancialLiteracyTool.Pages.UserPages
{
    [Authorize]
    public class BrowseUserAssessmentsModel : PageModel
    {
        public List<BrowseUserAssessment> Assessments { get; set; } = new();

        public void OnGet()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value);
                PopulateAssessments(userId);
            }
        }

        private void PopulateAssessments(int userId)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = @"
                    SELECT 
                        a.AssessmentID,
                        a.AssessmentName,
                        a.AssessmentDescription,
                        ua.UserAssessmentID,
                        ua.IsFinished
                    FROM UserAssessments ua
                    JOIN Assessment a
                        ON ua.AssessmentID = a.AssessmentID
                    WHERE ua.SystemUserID = @SystemUserID
                    ORDER BY a.AssessmentID";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@SystemUserID", userId);

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    bool isFinished = !reader.IsDBNull(4) && reader.GetBoolean(4);
                    int userAssessmentId = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);

                    Assessments.Add(new BrowseUserAssessment
                    {
                        AssessmentID = reader.GetInt32(0),
                        AssessmentName = reader.GetString(1),
                        AssessmentDescription = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        UserAssessmentID = userAssessmentId,
                        Status = isFinished ? "Completed" : "NotStarted",
                        ProgressPercent = isFinished ? 100 : 0
                    });
                }
            }
        }
    }
}