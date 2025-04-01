using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class AdminViewModel
    {
        public string UserId { get; set; }

        [Display(Name = "Username")]
        public string Username { get; set; }

        [Display(Name = "Admin ID")]
        public string AdminId { get; set; }
    }
}