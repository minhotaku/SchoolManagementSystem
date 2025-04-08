using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class StudentViewModel
    {
        public string UserId { get; set; }

        [Display(Name = "Username")]
        public string Username { get; set; }

        [Display(Name = "Student Id")]
        public string StudentId { get; set; }
        public string SchoolProgramId { get; set; }

        [Display(Name = "School Program Name")]
        public string SchoolProgramName { get; set; }
    }
}