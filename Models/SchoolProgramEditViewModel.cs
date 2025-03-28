using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class SchoolProgramEditViewModel
    {
        [Required]
        [Display(Name = "Mã Chương trình")]
        public string SchoolProgramId { get; set; } // Readonly

        [Required(ErrorMessage = "Tên chương trình học không được để trống.")]
        [Display(Name = "Tên Chương trình học")]
        public string SchoolProgramName { get; set; }
    }
}