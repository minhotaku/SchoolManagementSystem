using Microsoft.AspNetCore.Http; // Required for HttpContext.Session
using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Services.Interfaces;
using SchoolManagementSystem.Services.Implementation; // Để lấy GetInstance nếu không dùng DI
using SchoolManagementSystem.ViewModels; // Thêm namespace cho LoginViewModel
using SchoolManagementSystem.Data; // Thêm namespace cho UnitOfWork nếu cần

namespace SchoolManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthenticationService _authenticationService;
        // Không cần _unitOfWork ở đây nếu service đã xử lý hết logic DB

        // Session Key Constants
        private const string SessionKeyUserId = "_UserId";
        private const string SessionKeyUsername = "_Username";
        private const string SessionKeyUserRole = "_UserRole";

        public AccountController()
        {
            // Lấy instance Service theo mẫu Singleton
            _authenticationService = AuthenticationService.GetInstance();
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken] // Important for security
        public IActionResult Login(LoginViewModel model, string returnUrl = null)
        {
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
                    // ---

                    // Logged in successfully
                    // Chuyển hướng đến trang được yêu cầu hoặc trang chủ
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        // Chuyển hướng đến trang mặc định sau khi login, ví dụ: Home/Index
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    // Authentication failed
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
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
            // Chuyển hướng về trang chủ hoặc trang Login
            return RedirectToAction("Index", "Home");
        }

        // Optional: Action để hiển thị trang Access Denied (nếu cần sau này)
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}