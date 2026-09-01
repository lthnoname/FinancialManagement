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
            
            bool usernameExists = await _context.Users.AnyAsync(u=>u.Username == username);

            if (usernameExists)
            {
                ModelState.AddModelError(
                    "Username",
                    "Tên đăng nhập này đã tồn tại"
                    );
                return View(model);
            }

            bool emailExists = await _context.Users.AnyAsync(e => e.Email == email);
            if (emailExists)
            {
                ModelState.AddModelError(
                       "Email",
                       "Email này đã tồn tại"
                    );
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

            user.PasswordHash =
                _passwordHasher.HashPassword(user, model.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đăng ký tài khoản thành công";

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string login = model.UsernameOrEmail.Trim();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == login || u.Username == login);

            if(user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Tên đăng nhập hoặc email không chính xác"
                );
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(
                    "",
                    "Tài khoản đã bị vô hiệu hóa"
                    );
                return View(model);
            }

            var result = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                model.Password
            );

            if(result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    "",
                    "Tên đăng nhập hoặc mật khẩu không chính xác"
                );
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.Name,
                    user.Username
                ),

                new Claim(
                    ClaimTypes.Email,
                    user.Email
                ),
                new Claim(
                    "FullName",
                    user.FullName ?? user.Username
                )
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return RedirectToAction("Login", "Account");
        }
    }
}
