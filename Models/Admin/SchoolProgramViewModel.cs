using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class SchoolProgramViewModel
    {
        [Display(Name = "Mã Chương trình")]
        public string SchoolProgramId { get; set; }

        [Display(Name = "Tên Chương trình học")]
        public string SchoolProgramName { get; set; }

        [Display(Name = "Số lượng SV")]
        public int StudentCount { get; set; } = 0;
    }
}