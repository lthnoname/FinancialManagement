using FinancialManagement.Data;
using FinancialManagement.Models;
using FinancialManagement.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;
namespace FinancialManagement.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
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

        public async Task<IActionResult> Index()
        {
            int id = GetCurrentUserId();
            var now = DateTime.Now;

            //1. khoảng thời gian tháng hiện tại
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);

            // Khoảng thời gian tháng trước (để so sánh % tăng trưởng)
            var startOfLastMonth = startOfMonth.AddMonths(-1);
            var endOfLastMonth = startOfMonth.AddTicks(-1);

            var userTransactionsQuery = _context.Transactions.Where(t => t.UserId == id);

            //2. Tính tổng số dư khả dụng (toàn bộ thời gian)
            decimal totalIncomeAllTime = await userTransactionsQuery
                .Where(t => t.Type == "I")
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;
            decimal totalExpenseAllTime = await userTransactionsQuery
                .Where(t => t.Type == "E")
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;
            decimal totalBalance = totalIncomeAllTime - totalExpenseAllTime;

            // 3. Số liệu tháng này
            var currentMonthTransactions = await _context.Transactions
                .Where(t => t.UserId == id && t.TransactionDate >= startOfMonth && t.TransactionDate <= endOfMonth)
                .ToListAsync();
            decimal monthlyIncome = currentMonthTransactions.Where(t => t.Type == "I").Sum(t => t.Amount);
            decimal monthlyExpense = currentMonthTransactions.Where(t => t.Type == "E").Sum(t => t.Amount);
            int incomeCount = currentMonthTransactions.Count(t => t.Type == "I");
            int expenseCount = currentMonthTransactions.Count(t => t.Type == "E");

            // 4. số liệu tháng trước và tăng trưởng
            var lastMonthTransactions = await _context.Transactions
                .Where(t => t.UserId == id && t.TransactionDate >= startOfLastMonth && t.TransactionDate <= endOfLastMonth)
                .ToListAsync();
            decimal lastMonthIncome = lastMonthTransactions.Where(t => t.Type == "I").Sum(t => t.Amount);
            decimal lastMonthExpense = lastMonthTransactions.Where(t => t.Type == "E").Sum(t => t.Amount);
            decimal lastMonthNet = lastMonthIncome - lastMonthExpense;
            decimal currentMonthNet = monthlyIncome - monthlyExpense;

            double balanceGrowthRate = 0;
            if(lastMonthNet != 0)
            {
                balanceGrowthRate = (double)((currentMonthNet - lastMonthNet) / Math.Abs(lastMonthNet)) * 100;
            }
            else if(currentMonthNet > 0)
            {
                balanceGrowthRate = 100; // Tăng trưởng 100% nếu tháng trước là 0 và tháng này có lợi nhuận
            }

            double incomeGrowthRate = 0;
            if(lastMonthIncome > 0)
            {
                incomeGrowthRate = (double)((monthlyIncome - lastMonthIncome) / lastMonthIncome) * 100;
            }

            // 5. Dữ liệu biểu đồ dòng tiền 6 tháng gần nhất (tính từ 5 tháng trước tới nay)
            var cashFlowLabels = new List<string>();
            var cashFlowIncomes = new List<decimal>();
            var cashFlowExpenses = new List<decimal>();

            var sixMonthsAgo = new DateTime(now.Year, now.Month, 1).AddMonths(-5);
            var sixMonthsTxs = await userTransactionsQuery
                .Where(t => t.TransactionDate >= sixMonthsAgo && t.TransactionDate <= endOfMonth)
                .ToListAsync();
           
            for(int i = 5; i >= 0; i--)
            {
                var monthDate = now.AddMonths(-i);
                cashFlowLabels.Add(monthDate.ToString("MMM yyyy"));

                var monthData = sixMonthsTxs
                    .Where(t => t.TransactionDate.Month == monthDate.Month && t.TransactionDate.Year == monthDate.Year)
                    .ToList();

                cashFlowIncomes.Add(monthData.Where(t => t.Type == "I").Sum(t => t.Amount));
                cashFlowExpenses.Add(monthData.Where(t => t.Type == "E").Sum(t => t.Amount));
            }

            // 6. Cơ cấu chi tiêu theo Danh mục trong tháng này
            var expenseByCategories = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == id && t.Type == "E" && t.TransactionDate >= startOfMonth && t.TransactionDate <= endOfMonth)
                .GroupBy(t => t.Category != null ? t.Category.CategoryName : "Khác")
                .Select(g => new
                {
                    CategoryName = g.Key,
                    Total = g.Sum(t => t.Amount)
                })
                .OrderByDescending(x => x.Total)
                .ToListAsync();

            var expenseLabels = expenseByCategories.Select(x => x.CategoryName).ToList();
            var expenseAmounts = expenseByCategories.Select(x => x.Total).ToList();
            var expensePercentages = monthlyExpense > 0
                ? expenseByCategories.Select(x => Math.Round((double)(x.Total / monthlyExpense) * 100, 1)).ToList()
                : new List<double>();

            // 7. Top 5 giao dịch gần đây nhất
            var recentTransactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == id)
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.CreatedAt)
                .Take(5)
                .ToListAsync();

            // 8. Danh mục của User (phục vụ Modal thêm giao dịch nhanh)
            var categories = await _context.Categories
                .Where(c => c.UserId == id)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
            var viewModel = new DashboardViewModel
            {
                TotalBalance = totalBalance,
                MonthlyIncome = monthlyIncome,
                MonthlyExpense = monthlyExpense,
                IncomeCount = incomeCount,
                ExpenseCount = expenseCount,
                balanceGrowthRate = Math.Round(balanceGrowthRate, 1),
                incomeGrowthRate = Math.Round(incomeGrowthRate, 1),
                CashFlowLabels = cashFlowLabels,
                CashFlowIncomes = cashFlowIncomes,
                CashFlowExpenses = cashFlowExpenses,
                ExpenseCategoryLabels = expenseLabels,
                ExpenseCategoryAmounts = expenseAmounts,
                ExpenseCategoryPercentages = expensePercentages,
                RecentTransactions = recentTransactions,
                Categories = categories
            };
            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
