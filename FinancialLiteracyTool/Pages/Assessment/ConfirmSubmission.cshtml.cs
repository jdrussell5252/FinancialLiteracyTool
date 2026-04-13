using FinancialLiteracyTool.Model.Questions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace FinancialLiteracyTool.Pages.Assessment
{
    public class ConfirmSubmissionModel : PageModel
    {
        public int TotalQuestions { get; private set; }
        public int AnsweredCount { get; private set; }
        public bool AllAnswered => AnsweredCount >= TotalQuestions;
        public int CurrAssessmentID { get; set; }

        public IActionResult OnGet(int id, int NumQuestions)
        {
            CurrAssessmentID = id;
            TotalQuestions = NumQuestions;
            TempData.Keep("AnswersJson");

            var json = TempData["AnswersJson"] as string;

            if (string.IsNullOrWhiteSpace(json))
            {
                return RedirectToPage("/Assessment/TakeAssessment");
            }

            var answers = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();

            AnsweredCount = answers.Values.Count(v => !string.IsNullOrWhiteSpace(v));

            return Page();
        }

        public IActionResult OnPost(int id)
        {
            TempData.Keep("AnswersJson");

            var json = TempData["AnswersJson"] as string;
            if (string.IsNullOrWhiteSpace(json))
                return RedirectToPage("/Assessment/TakeAssessment");

            var answers = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            var answeredCount = answers.Values.Count(v => !string.IsNullOrWhiteSpace(v));

            if (answeredCount < TotalQuestions)
            {
                // stay on confirm page; button is disabled anyway
                return RedirectToPage("/Assessment/ConfirmSubmission");
            }

            // TODO: Save + score here safely
            return RedirectToPage("/Assessment/SubmissionSuccess", new { assessmentId = id });
        }
    }
}
