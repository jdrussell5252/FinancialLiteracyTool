using FinancialLiteracyTool.MyAppHelper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace FinancialLiteracyTool.Pages.AdminPages
{
    public class StatsModel : PageModel
    {
        
        public int TotalUsers { get; set; }
        public int AssessmentsTaken { get; set; }
        public int Completed { get; set; }
        public int InProgress { get; set; }
        public double CompletionRate { get; set; }
        public double AverageScore { get; set; }
        public double AverageProgress { get; set; }
        public void OnGet()
        {
            string connStr = AppHelper.GetDBConnectionString();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                // Total Users
                SqlCommand cmd1 = new SqlCommand("SELECT COUNT(*) FROM dbo.SystemUser", conn);
                TotalUsers = Convert.ToInt32(cmd1.ExecuteScalar() ?? 0);

                // Assessments Taken
                SqlCommand cmd2 = new SqlCommand("SELECT COUNT(*) FROM dbo.UserAssessments", conn);
                AssessmentsTaken = Convert.ToInt32(cmd2.ExecuteScalar() ?? 0);

                // Completed
                SqlCommand cmd3 = new SqlCommand("SELECT COUNT(*) FROM dbo.UserAssessments WHERE IsFinished = 1", conn);
                Completed = Convert.ToInt32(cmd3.ExecuteScalar() ?? 0);

                // In Progress
                SqlCommand cmd4 = new SqlCommand("SELECT COUNT(*) FROM dbo.UserAssessments WHERE IsFinished = 0", conn);
                InProgress = Convert.ToInt32(cmd4.ExecuteScalar() ?? 0);

                // Completion Rate
                SqlCommand cmd5 = new SqlCommand(@"SELECT 
                                                COUNT(CASE WHEN IsFinished = 1 THEN 1 END) * 100.0 / COUNT(*) 
                                                FROM dbo.UserAssessments", conn);

                CompletionRate = Convert.ToDouble(cmd5.ExecuteScalar() ?? 0);

                // Average Score
                SqlCommand cmd6 = new SqlCommand("SELECT AVG(CAST(Result AS FLOAT)) FROM dbo.Results", conn);
                AverageScore = Convert.ToDouble(cmd6.ExecuteScalar() ?? 0);

            }
        }
    }
}
