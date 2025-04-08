using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class SchoolProgramEditViewModel
    {
        [Required]
        [Display(Name = "Program ID")]
        public string SchoolProgramId { get; set; }

        [Required(ErrorMessage = "School program name cannot be empty.")]
        [Display(Name = "School Program Name")]
        public string SchoolProgramName { get; set; }
    }
}