namespace FinancialLiteracyTool.Model.Questions
{
    public class Question
    {
        public int QuestionID { get; set; }
        public int AssessmentID { get; set; }
        public string QuestionText { get; set; }
        public int QuestionAreaID { get; set; }
        public string QuestionArea { get; set; }
        public string QuestionType { get; set; }
        public int QuestionTypeID { get; set; }
        public List<QuestionChoices> Choices { get; set; } = new();
        public int CurrQuestionIndex { get; set; }
    }// End of 'Question' Class.
}// End of 'namespace'.
