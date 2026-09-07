using FinancialManagement.Data;
using FinancialManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace FinancialManagement.Controllers
{
    [Authorize] //Bắt buộc người dùng phải đăng nhập để truy cập vào các action trong controller này
    public class CategoryController : Controller
    {
        private readonly AppDbContext _context;

        public CategoryController(AppDbContext context)
        {
            _context = context;
        }

        //Helper lấy UserId của người dùng hiện tại
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }

            // Dự phòng nếu cookie cũ chưa có NameIdentifier
            var username = User.Identity?.Name;
            return _context.Users.Where(u => u.Username == username).Select(u => u.UserId).FirstOrDefault();
        }

        // GET: /Category/
        public async Task<IActionResult> Index()
        {
            int userId = GetCurrentUserId();
            // Lấy danh sách danh mục kèm số lượng giao dịch liên quan
            var categories = await _context.Categories
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.Type)
                .ThenBy(c=>c.CategoryName)
                .Select(c => new
                {
                    Category = c,
                    TransactionCount = c.Transactions.Count()
                })
                .ToListAsync();
            ViewBag.CategoriesWithCount = categories;
            return View(categories.Select(x=>x.Category).ToList());
        }

        // POST: /Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(String categoryName, string type)
        {
            int userId = GetCurrentUserId();
            categoryName = categoryName?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(categoryName))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập tên danh mục";
                return RedirectToAction(nameof(Index));
            }

            //kiểm tra trùng lặp
            bool exists = await _context.Categories.AnyAsync(c=>
                    c.UserId == userId &&
                    c.CategoryName.ToLower() == categoryName.ToLower() &&
                    c.Type == type);

            if (exists)
            {
                TempData["ErrorMessage"] = $"Danh mục '{categoryName}' đã tồn tại trong danh sách {(type == "I" ? "Thu nhập" : "Chi tiêu")}.";
                return RedirectToAction(nameof(Index));
            }

            var newCategory = new Category
            {
                UserId = userId,
                CategoryName = categoryName,
                Type = type,
                CreatedAt = DateTime.Now,
            };

            _context.Categories.Add(newCategory);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã thêm danh mục '{categoryName}' thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
