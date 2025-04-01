using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SchoolManagementSystem.Models
{
    public class StudentEditViewModel
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        [Display(Name = "Student Id")]
        public string StudentId { get; set; }

        [Required(ErrorMessage = "Username cannot be empty.")]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Please select a school program.")]
        [Display(Name = "School Program")]
        public string SchoolProgramId { get; set; }

        [Display(Name = "New password (leave blank to not change)")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        public SelectList? SchoolProgramList { get; set; }
    }
}