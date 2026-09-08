using FinancialManagement.Data;
using FinancialManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace FinancialManagement.Controllers
{
    [Authorize]
    public class TransactionController : Controller
    {
        private readonly AppDbContext _context;
        public TransactionController(AppDbContext context)
        {
            _context = context;
        }

        //Helper lấy UserId của người dùng hiện tại
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            // Dự phòng nếu cookie cũ chưa có NameIdentifier
            var username = User.Identity?.Name;
            return _context.Users.Where(u => u.Username == username).Select(u => u.UserId).FirstOrDefault();
        }

        //GET: /Transaction/
        public async Task<IActionResult> Index(string? type, int? categoryId, DateTime? fromDate, DateTime? toDate)
        {
            int userId = GetCurrentUserId();

            //Mặc định xem tháng hiện tại nếu không chọn ngày
            var start = fromDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var end = toDate ?? DateTime.Now.Date.AddDays(1).AddTicks(-1);

            //Query giao dịch
            var query = _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == userId && t.TransactionDate >= start && t.TransactionDate <= end);

            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(t => t.Type == type);
            }
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(t => t.CategoryId == categoryId.Value);
            }

            var transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();

            // Tính toán tổng số liệu trong kỳ hiển thị
            decimal totalIncome = transactions.Where(t => t.Type == "I").Sum(t => t.Amount);
            decimal totalExpense = transactions.Where(t => t.Type == "E").Sum(t => t.Amount);
            decimal netBalance = totalIncome - totalExpense;
            ViewBag.TotalIncome = totalIncome;
            ViewBag.TotalExpense = totalExpense;
            ViewBag.NetBalance = netBalance;

            // Dữ liệu phục vụ bộ lọc và modal tạo/sửa
            ViewBag.SelectedType = type;
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.FromDate = start.ToString("yyyy-MM-dd");
            ViewBag.ToDate = end.ToString("yyyy-MM-dd");

            // Danh sách Category của user hiện tại
            var userCategories = await _context.Categories
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
            ViewBag.Categories = userCategories;
            return View(transactions);
        }

        // POST: /Transaction/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int categoryId, decimal amount, DateTime transactionDate, string? note)
        {
            int userId = GetCurrentUserId();
            
            if(amount <= 0)
            {
                TempData["ErrorMessage"] = "Số tiền phải lớn hơn 0";
                return RedirectToAction(nameof(Index));
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == categoryId && c.UserId == userId);

            if(category == null)
            {
                TempData["ErrorMessage"] = "Danh mục không hợp lệ hoặc không thuộc tài khoản của bạn.";
                return RedirectToAction(nameof(Index));
            }

            var transaction = new Transaction
            {
                UserId = userId,
                CategoryId = categoryId,
                Amount = amount,
                Type = category.Type,
                TransactionDate = transactionDate,
                Note = note?.Trim() ?? string.Empty,
                CreatedAt = DateTime.Now
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Giao dịch đã được tạo thành công.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Transaction/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int transactionId, int categoryId, decimal amount, DateTime transactionDate, string? note)
        {
            int userId = GetCurrentUserId();
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId && t.UserId == userId);

            if(transaction == null)
            {
                TempData["ErrorMessage"] = "Giao dịch không tồn tại hoặc không thuộc tài khoản của bạn.";
                return RedirectToAction(nameof(Index));
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == categoryId && c.UserId == userId);
            if(category == null)
            {
                TempData["ErrorMessage"] = "Danh mục không hợp lệ hoặc không thuộc tài khoản của bạn.";
                return RedirectToAction(nameof(Index));
            }

            if(amount <= 0)
            {
                TempData["ErrorMessage"] = "Số tiền phải lớn hơn 0";
                return RedirectToAction(nameof(Index));
            }

            transaction.CategoryId = categoryId;
            transaction.Type = category.Type;
            transaction.Amount = amount;
            transaction.TransactionDate = transactionDate;
            transaction.Note = note?.Trim() ?? string.Empty;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Giao dịch đã được cập nhật thành công.";
            return RedirectToAction(nameof(Index));
        }

        //DELETE: /Transaction/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int transactionId)
        {
            int userId = GetCurrentUserId();
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId && t.UserId == userId);
            
            if(transaction == null)
            {
                TempData["ErrorMessage"] = "Giao dịch không tồn tại hoặc không thuộc tài khoản của bạn.";
                return RedirectToAction(nameof(Index));
            }

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Giao dịch đã được xóa thành công.";
            return RedirectToAction(nameof(Index));
        }

    }
}
