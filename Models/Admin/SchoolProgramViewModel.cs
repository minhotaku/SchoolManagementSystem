using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class SchoolProgramViewModel
    {
        [Display(Name = "Program ID")]
        public string SchoolProgramId { get; set; }

        [Display(Name = "School Program Name")]
        public string SchoolProgramName { get; set; }

        [Display(Name = "Student Count")]
        public int StudentCount { get; set; } = 0;
    }
}