using FinancialLiteracyTool.Model.Questions;
using FinancialLiteracyTool.MyAppHelper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Text.Json;

namespace FinancialLiteracyTool.Pages.Assessment
{
    //[Authorize]
    public class ConfirmSubmissionModel : PageModel
    {
        public int TotalQuestions { get; private set; }
        public int AnsweredCount { get; private set; }
        public bool AllAnswered => AnsweredCount >= TotalQuestions;
        public int CurrAssessmentID { get; set; }
        public int CurrUserAssessmentID { get; set; }

        public IActionResult OnGet(int id, int userAssessmentID, int NumQuestions)
        {
            CurrAssessmentID = id;
            CurrUserAssessmentID = userAssessmentID;
            TotalQuestions = NumQuestions;
            TempData.Keep("AnswersJson");

            var json = TempData["AnswersJson"] as string;

            if (string.IsNullOrWhiteSpace(json))
            {
                return RedirectToPage("/UserPages/TakeAssessment");
            }

            var answers = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();

            AnsweredCount = answers.Values.Count(v => !string.IsNullOrWhiteSpace(v));

            return Page();
        }

        public IActionResult OnPost(int id, int userAssessmentID)
        {
            TempData.Keep("AnswersJson");

            var json = TempData["AnswersJson"] as string;
            if (string.IsNullOrWhiteSpace(json))
                return RedirectToPage("/UserPages/TakeAssessment");

            var answers = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            var answeredCount = answers.Values.Count(v => !string.IsNullOrWhiteSpace(v));

            if (answeredCount < TotalQuestions)
                return RedirectToPage("/UserPages/ConfirmSubmission");

            MarkAssessmentFinished(userAssessmentID);

            return RedirectToPage("/UserPages/SubmissionSuccess", new { assessmentId = id });
        }

        private void MarkAssessmentFinished(int userAssessmentID)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "UPDATE UserAssessments SET IsFinished = 1 WHERE UserAssessmentID = @UserAssessmentID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UserAssessmentID", userAssessmentID);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
