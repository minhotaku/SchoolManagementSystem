using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering; // Cho SelectList

namespace SchoolManagementSystem.Models
{
    public class CourseCreateViewModel
    {

        [Required(ErrorMessage = "Tên khóa học không được để trống.")]
        [Display(Name = "Tên Khóa học")]
        public string CourseName { get; set; }

        [Required(ErrorMessage = "Số tín chỉ không được để trống.")]
        [Range(1, 10, ErrorMessage = "Số tín chỉ phải từ 1 đến 10.")]
        [Display(Name = "Số tín chỉ")]
        public int Credits { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giảng viên phụ trách.")]
        [Display(Name = "Giảng viên phụ trách")]
        public string FacultyId { get; set; }

        public SelectList? FacultyList { get; set; }
    }
}