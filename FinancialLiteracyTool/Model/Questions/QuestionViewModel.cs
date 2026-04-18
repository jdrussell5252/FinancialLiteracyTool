using System.Collections.Generic;

namespace FinancialLiteracyTool.Model.Questions
{
    public class QuestionViewModel
    {
        public int QuestionID { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public int AreaID { get; set; }
        public string AreaName { get; set; } = string.Empty;
        public int QuestionTypeID { get; set; }
        public string QuestionType { get; set; } = string.Empty;
        public List<QuestionChoices> Choices { get; set; } = new();
    }
}