using System.ComponentModel.DataAnnotations;

namespace FinancialLiteracyTool.Model.Questions
{
    public class QuestionView
    {
        public int QuestionID { get; set; }
        [Required(ErrorMessage = "Question Text is required.")]
        public string? QuestionText { get; set; }
        public string? QuestionArea { get; set; }
    }// End of 'QuestionView' Class.
}// End of 'namespace'.
