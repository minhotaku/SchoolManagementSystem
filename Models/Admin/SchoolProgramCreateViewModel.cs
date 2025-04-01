using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class SchoolProgramCreateViewModel
    {

        [Required(ErrorMessage = "School program name cannot be empty.")]
        [Display(Name = "School Program Name")]
        public string SchoolProgramName { get; set; }
    }
}