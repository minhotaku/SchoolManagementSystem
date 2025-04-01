using Microsoft.AspNetCore.Http; // Required for HttpContext.Session
using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Services.Interfaces;
using SchoolManagementSystem.Services.Implementation; // Để lấy GetInstance nếu không dùng DI
using SchoolManagementSystem.ViewModels; // Thêm namespace cho LoginViewModel
using SchoolManagementSystem.Data; // Thêm namespace cho UnitOfWork nếu cần
using SchoolManagementSystem.Utils;
using SchoolManagementSystem.Entities;
using SchoolManagementSystem.Models;

namespace SchoolManagementSystem.Controllers
{
    public class AccountController : BaseController
    {
        private readonly IAuthenticationService _authenticationService;

        // Session Key Constants
        private const string SessionKeyUserId = "_UserId";
        private const string SessionKeyUsername = "_Username";
        private const string SessionKeyUserRole = "_UserRole";

        public AccountController()
        {
            _authenticationService = AuthenticationService.GetInstance();
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            // Check if user is already logged in
            if (HttpContext.Session.GetString(SessionKeyUserId) != null)
            {
                // User is already logged in, redirect based on their role
                string userRole = HttpContext.Session.GetString(SessionKeyUserRole);

                switch (userRole)
                {
                    case RoleConstants.Admin:
                        return RedirectToAction("Index", "AdminDashboard");
                    case RoleConstants.Faculty:
                        return RedirectToAction("Index", "Faculty");
                    case RoleConstants.Student:
                        return RedirectToAction("Dashboard", "Student");
                    default:
                        // If role is somehow invalid, proceed to home or some default page
                        return RedirectToAction("Index", "Home");
                }
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken] // Important for security
        public IActionResult Login(LoginViewModel model, string returnUrl = null)
        {
            // Check if user is already logged in
            if (HttpContext.Session.GetString(SessionKeyUserId) != null)
            {
                // User is already logged in, redirect based on their role
                string userRole = HttpContext.Session.GetString(SessionKeyUserRole);

                switch (userRole)
                {
                    case RoleConstants.Admin:
                        return RedirectToAction("Index", "AdminDashboard");
                    case RoleConstants.Faculty:
                        return RedirectToAction("Index", "Faculty");
                    case RoleConstants.Student:
                        return RedirectToAction("Dashboard", "Student");
                    default:
                        // If role is somehow invalid, proceed to home or some default page
                        return RedirectToAction("Index", "Home");
                }
            }

            // Existing login logic for users who aren't logged in
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = _authenticationService.Authenticate(model.Username, model.Password);

                if (user != null)
                {
                    // --- Lưu thông tin vào Session ---
                    HttpContext.Session.SetString(SessionKeyUserId, user.UserId);
                    HttpContext.Session.SetString(SessionKeyUsername, user.Username);
                    HttpContext.Session.SetString(SessionKeyUserRole, user.Role);

                    _sessionService.SetUserSession(user.UserId, user.Username, user.Role); // Logic hiện tại cần để phân quyền HTTP Sesion phía trên đã lỗi thời

                    // If returnUrl is specified, redirect there
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    // Otherwise redirect based on role
                    switch (user.Role)
                    {
                        case RoleConstants.Admin:
                            return RedirectToAction("Index", "AdminDashboard");
                        case RoleConstants.Faculty:
                            return RedirectToAction("Index", "Faculty");
                        case RoleConstants.Student:
                            return RedirectToAction("Dashboard", "Student");
                        default:
                            return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    // Authentication failed
                    ModelState.AddModelError(string.Empty, "Incorrect login information.");
                    return View(model);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            // Xóa Session
            HttpContext.Session.Clear();
            _sessionService.ClearSession();

            // Chuyển hướng về trang Login
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}