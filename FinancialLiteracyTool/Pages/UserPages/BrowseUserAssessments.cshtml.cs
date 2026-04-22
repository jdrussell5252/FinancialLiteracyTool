using FinancialLiteracyTool.Model.Assessments;
using FinancialLiteracyTool.MyAppHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace FinancialLiteracyTool.Pages.UserPages
{
    [Authorize]
    public class BrowseUserAssessmentsModel : PageModel
    {
        public List<BrowseUserAssessment> Assessments { get; set; } = new();
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 5;
        public int TotalCount { get; set; }
        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize)));
        public int AssessmentID { get; set; }
        public string? AssessmentName { get; set; }
        public string? AssessmentDescription { get; set; }
        public string? Status { get; set; } 
        public int ProgressPercent { get; set; }
        // public int UserAssessmentID { get; set; }


        public void OnGet(int pageNumber = 1, int pageSize = 5)
        {
            // Safely access the NameIdentifier claim
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            /*--------------------ADMIN PRIV----------------------*/
            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value); // Use the claim value only if it exists
                PopulateAssessments(userId);
            }

            // === Pagination logic ===
            PageNumber = pageNumber < 1 ? 1 : pageNumber;
            PageSize = pageSize < 1 ? 5 : pageSize;

            TotalCount = Assessments.Count;

            // Clamp PageNumber so it’s not past the last page
            if (TotalCount > 0 && (PageNumber - 1) * PageSize >= TotalCount)
            {
                PageNumber = (int)Math.Ceiling((double)TotalCount / PageSize);
            }

            if (TotalCount > 0)
            {
                int skip = (PageNumber - 1) * PageSize;
                Assessments = Assessments
                    .Skip(skip)
                    .Take(PageSize)
                    .ToList();
            }
        }//End of ''.

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
                       ua.IsFinished,
                       (SELECT COUNT(*) FROM AssessmentQuestion aq WHERE aq.AssessmentID = a.AssessmentID) AS TotalQuestions
                   FROM Assessment AS a
                   LEFT JOIN UserAssessments AS ua 
                       ON ua.AssessmentID = a.AssessmentID
                   WHERE ua.SystemUserID = @SystemUserID
                   ORDER BY a.AssessmentID";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@SystemUserID", userId);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                // NEEDS FIXING, 
                while (reader.Read())
                {                
                    bool? isFinished = reader.IsDBNull(4) ? false : (bool?)reader.GetBoolean(4);
                    int totalQuestions = reader.IsDBNull(5) ? 1 : reader.GetInt32(5);
                    //int currentIndex = reader.IsDBNull(4) ? 0 : int.Parse(reader.GetString(4));

                    string status = isFinished == false ? "NotStarted"
                                  : isFinished == true ? "Completed"
                                  : "InProgress";

                    int AssessmentID = reader.GetInt32(0);
                    string AssessmentName = reader.GetString(1);
                    string AssessmentDescription = reader.IsDBNull(2) ? "" : reader.GetString(2);
                    int UserAssessmentID = reader.GetInt32(3);


                    // int progress = status == "NotStarted" ? 0
                    //             : status == "Completed" ? 100
                    //             : (int)Math.Round((double)currentIndex / totalQuestions * 100);
                    Assessments.Add(new BrowseUserAssessment
                    {
                        AssessmentID = reader.GetInt32(0),
                        AssessmentName = reader.GetString(1),
                        AssessmentDescription = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        UserAssessmentID = reader.GetInt32(3),
                        Status = status,
                        // ProgressPercent = progress
                    });
                }
            }
        }// End of 'PopulateAssessments'.
    }// End of 'BrowseUserAssessments' Class.
}// End of 'namespace'.
