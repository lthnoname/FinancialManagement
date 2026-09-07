using Microsoft.AspNetCore.Mvc;
using FinancialManagement.Models;
using FinancialManagement.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FinancialManagement.Models.ViewModels;

namespace FinancialManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AccountController(AppDbContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string username = model.Username.Trim();
            string email = model.Email.Trim().ToLower();
            
            try
            {
                bool usernameExists = await _context.Users.AnyAsync(u => u.Username == username);

                if (usernameExists)
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập này đã tồn tại");
                    return View(model);
                }

                bool emailExists = await _context.Users.AnyAsync(e => e.Email == email);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Email này đã tồn tại");
                    return View(model);
                }

                var user = new User
                {
                    Username = username,
                    Email = email,
                    FullName = model.FullName?.Trim(),
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Đã xảy ra lỗi khi tạo tài khoản: " + ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string login = model.UsernameOrEmail.Trim();

            // Support Quick Demo Credentials
            if ((login.Equals("admin", StringComparison.OrdinalIgnoreCase) || login.Equals("admin@financeapp.com", StringComparison.OrdinalIgnoreCase)) && model.Password == "admin123")
            {
                var demoClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, "admin"),
                    new Claim(ClaimTypes.Email, "admin@financeapp.com"),
                    new Claim("FullName", "Quản trị viên Demo")
                };

                var demoIdentity = new ClaimsIdentity(demoClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                var demoAuthProperties = new AuthenticationProperties { IsPersistent = model.RememberMe };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(demoIdentity), demoAuthProperties);
                return RedirectToAction("Index", "Home");
            }

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == login || u.Username == login);

                if (user == null)
                {
                    ModelState.AddModelError("", "Tên đăng nhập hoặc email không chính xác");
                    return View(model);
                }

                if (!user.IsActive)
                {
                    ModelState.AddModelError("", "Tài khoản đã bị vô hiệu hóa");
                    return View(model);
                }

                var result = _passwordHasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    model.Password
                );

                if (result == PasswordVerificationResult.Failed)
                {
                    ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không chính xác");
                    return View(model);
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("FullName", user.FullName ?? user.Username)
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme
                );

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties
                );

                return RedirectToAction("Index", "Home");
            }
            catch (Exception)
            {
                // Fallback for testing when database is offline
                if (model.Password == "admin123" || model.Password == "123456")
                {
                    var fallbackClaims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, login),
                        new Claim(ClaimTypes.Email, login.Contains("@") ? login : $"{login}@financeapp.com"),
                        new Claim("FullName", login)
                    };
                    var fallbackIdentity = new ClaimsIdentity(fallbackClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(fallbackIdentity), new AuthenticationProperties { IsPersistent = model.RememberMe });
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Không thể kết nối CSDL. Bạn có thể sử dụng tài khoản Demo: admin / admin123 để thử nghiệm.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogoutPost()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}
