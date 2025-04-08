using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class UserViewModel
    {
        public string UserId { get; set; }

        [Display(Name = "Username")]
        public string Username { get; set; }

        [Display(Name = "Role")]
        public string Role { get; set; }

        [Display(Name = "School Program")]
        public string SchoolProgramName { get; set; }

        [Display(Name = "Created Date")]
        public string CreatedDate { get; set; }

        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }
    }
}