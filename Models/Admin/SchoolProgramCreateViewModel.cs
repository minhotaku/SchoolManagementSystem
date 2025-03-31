using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class SchoolProgramCreateViewModel
    {

        [Required(ErrorMessage = "Tên chương trình học không được để trống.")]
        [Display(Name = "Tên Chương trình học")]
        public string SchoolProgramName { get; set; }
    }
}