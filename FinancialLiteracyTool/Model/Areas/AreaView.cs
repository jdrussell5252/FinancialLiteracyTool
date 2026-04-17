using System.ComponentModel.DataAnnotations;

namespace FinancialLiteracyTool.Model.Areas
{
    public class AreaView
    {
        public int AreaID { get; set; }
        [Required(ErrorMessage = "Area Name is required.")]
        public string AreaName { get; set; }
    }
}
