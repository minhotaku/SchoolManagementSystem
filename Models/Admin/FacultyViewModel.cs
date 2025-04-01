using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class FacultyViewModel
    {
        public string UserId { get; set; }

        [Display(Name = "Username")]
        public string Username { get; set; }

        [Display(Name = "Faculty ID")]
        public string FacultyId { get; set; }

    }
}