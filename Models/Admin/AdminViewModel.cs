using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class AdminViewModel
    {
        public string UserId { get; set; }

        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; }

        [Display(Name = "Mã Admin")]
        public string AdminId { get; set; }
    }
}