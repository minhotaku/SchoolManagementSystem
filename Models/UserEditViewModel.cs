﻿using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class UserEditViewModel
    {
        public string UserId { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; }

        [Display(Name = "Mật khẩu mới (để trống nếu không đổi)")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Required]
        public string CurrentRole { get; set; }

        [Display(Name = "Vai trò")]
        public string Role => CurrentRole;

        [Display(Name = "Chương trình học")]
        public string? SchoolProgramId { get; set; }
    }
}