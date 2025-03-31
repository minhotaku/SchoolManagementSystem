using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SchoolManagementSystem.Models
{
    public class EnrollmentCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn sinh viên.")]
        [Display(Name = "Sinh viên")]
        public string StudentId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn khóa học.")]
        [Display(Name = "Khóa học")]
        public string CourseId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập học kỳ.")]
        [Display(Name = "Học kỳ")]
        [RegularExpression(@"^(Fall|Spring|Summer)\s\d{4}$", ErrorMessage = "Định dạng học kỳ không hợp lệ (vd: Fall 2024, Spring 2025).")]
        public string Semester { get; set; }

        public SelectList? StudentList { get; set; }
        public SelectList? CourseList { get; set; }
    }
}