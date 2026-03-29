using FinancialLiteracyTool.Model.Assessments;
using FinancialLiteracyTool.Model.Questions;
using FinancialLiteracyTool.Model.Users;
using FinancialLiteracyTool.MyAppHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Security.Claims;


namespace FinancialLiteracyTool.Pages.AdminPages.AdminAssessment
{
    [Authorize]
    public class BrowseAssessmentsModel : PageModel
    {
        public bool IsAdmin { get; set; }
        public List<BrowseAssessment> Assessments { get; set; } = new();
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 5;
        public int TotalCount { get; set; }
        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize)));
        
        public IActionResult OnGet(int id, int pageNumber = 1, int pageSize = 5)
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

            PopulateAssessments();

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

            return Page();
        }//End of 'OnGet'.

        // Delete handler: deletes the selected assessment and related rows.
        public IActionResult OnPostDelete(int id)
        {
            // Ensure user is authenticated and an admin
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                TempData["ErrorMessage"] = "You must be signed in to delete an assessment.";
                return RedirectToPage();
            }

            int userId = int.Parse(userIdClaim.Value);
            CheckIfUserIsAdmin(userId);
            if (!IsAdmin)
            {
                TempData["ErrorMessage"] = "Only administrators can delete assessments.";
                return RedirectToPage();
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {
                    conn.Open();

                    using (var tran = conn.BeginTransaction())
                    {
                        try
                        {
                            // Remove any question links first (FK constraints)
                            using (var deleteQ = new SqlCommand("DELETE FROM AssessmentQuestion WHERE AssessmentID = @AssessmentID", conn, tran))
                            {
                                deleteQ.Parameters.AddWithValue("@AssessmentID", id);
                                deleteQ.ExecuteNonQuery();
                            }

                            // Remove any area links
                            using (var deleteA = new SqlCommand("DELETE FROM AssessmentArea WHERE AssessmentID = @AssessmentID", conn, tran))
                            {
                                deleteA.Parameters.AddWithValue("@AssessmentID", id);
                                deleteA.ExecuteNonQuery();
                            }

                            // Finally remove the assessment itself
                            using (var deleteAssessment = new SqlCommand("DELETE FROM Assessment WHERE AssessmentID = @AssessmentID", conn, tran))
                            {
                                deleteAssessment.Parameters.AddWithValue("@AssessmentID", id);
                                int affected = deleteAssessment.ExecuteNonQuery();
                                if (affected == 0)
                                {
                                    tran.Rollback();
                                    TempData["ErrorMessage"] = "Assessment not found.";
                                    return RedirectToPage();
                                }
                            }

                            tran.Commit();
                        }
                        catch (Exception ex)
                        {
                            tran.Rollback();
                            TempData["ErrorMessage"] = $"Delete failed: {ex.Message}";
                            return RedirectToPage();
                        }
                    }
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Delete failed: {ex.Message}";
                return RedirectToPage();
            }
        }

        private void PopulateAssessments()
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                // include description for display on browse page
                string query = "SELECT AssessmentID, AssessmentName, AssessmentDescription FROM Assessment";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        BrowseAssessment aAssessment = new BrowseAssessment
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
    }// End of 'BrowseAssessments' Class.
}// End of 'namespace'.
