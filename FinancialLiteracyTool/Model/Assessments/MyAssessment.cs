using FinancialLiteracyTool.Model.Questions;
using System.ComponentModel.DataAnnotations;

namespace FinancialLiteracyTool.Model.Assessments
{
    public class MyAssessment
    {
        public int AssessmentID { get; set; }
        public int UserID { get; set; }

        [Required]
        [StringLength(100)] // match DB length 
        public string AssessmentName { get; set; }

        [Required]
        [StringLength(100)] // match DB length 
        public string AssessmentDescription { get; set; }
        //public List<Question> Questions { get; set; }
    }// End of 'Assessment' Class.
}// End of 'namespace'.
