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

        private void PopulateAssessments(int id)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                // include description for display on browse page
                string query = "SELECT a.AssessmentID, a.AssessmentName, a.AssessmentDescription FROM Assessment AS a INNER JOIN UserAssessments AS ua ON ua.SystemUserID = a.SystemUserID WHERE ua.SystemUserID = @SystemUserID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@SystemUserID", id);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        BrowseUserAssessment aAssessment = new BrowseUserAssessment
                        {
                            AssessmentID = reader.GetInt32(0),
                            AssessmentName = reader.GetString(1),
                            AssessmentDescription = reader.GetString(2)
                        };
                        Assessments.Add(aAssessment);

                    }
                }
            }
        }// End of 'PopulateAssessments'.
    }// End of 'BrowseUserAssessments' Class.
}// End of 'namespace'.
