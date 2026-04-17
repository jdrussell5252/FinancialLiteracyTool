using System.ComponentModel.DataAnnotations;

namespace FinancialLiteracyTool.Model.Areas
{
    public class MyAreas
    {
        [Required(ErrorMessage = "Area Name is required.")]
        public string AreaName { get; set; }
    }// End of 'MyAreas' Class.
}// End of 'namespace'.
