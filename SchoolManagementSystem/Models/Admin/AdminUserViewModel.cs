using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class AdminUserViewModel
    {
        public string UserId { get; set; }

        [Display(Name = "Username")]
        public string Username { get; set; }

        [Display(Name = "Role")]
        public string Role { get; set; } // "Student", "Faculty", "Admin"

        public string StudentId { get; set; }
        public string FacultyId { get; set; }
        public string AdminId { get; set; }

        public string SchoolProgramId { get; set; }

        [Display(Name = "School Program")]
        public string SchoolProgramName { get; set; }

    }
}