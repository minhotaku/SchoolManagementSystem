using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace SchoolManagementSystem.Models
{
    public class GradeComponentViewModel
    {
        public string GradeId { get; set; } = "";

        [Display(Name = "Component")]
        public string Component { get; set; } = "";

        [Display(Name = "Score")]
        [DisplayFormat(DataFormatString = "{0:N1}")]
        public decimal Score { get; set; }
        public string FormattedScore => Score.ToString("N1", CultureInfo.InvariantCulture);
    }
}