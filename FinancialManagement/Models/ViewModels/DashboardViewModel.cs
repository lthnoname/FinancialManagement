using FinancialManagement.Models;
namespace FinancialManagement.Models.ViewModels
{
    public class DashboardViewModel
    {
        //1. 4 thẻ chỉ số chính
        public decimal TotalBalance { get; set; }                               // Số dư khả dụng
        public decimal MonthlyIncome { get; set; }                              // Tổng thu nhập tháng này
        public decimal MonthlyExpense { get; set; }                             // Tổng chi tiêu tháng này
        public decimal MonthlySavings => MonthlyIncome - MonthlyExpense;        // Tích lũy tháng này

        public decimal IncomeCount { get; set; }                                //số khoảng thu tháng này
        public decimal ExpenseCount { get; set; }                               //số khoảng chi tháng này
        public double ExpenseRatio => MonthlyIncome > 0 ? (double)(MonthlyExpense / MonthlyIncome) * 100 : 0;   // % Chi tiêu trên thu nhập

        //2. Dữ liệu biểu đồ dòng tiền 6 tháng gần nhất (Bar chart)
        public List<string> CashFlowLabels { get; set; } = new();
        public List<decimal> CashFlowIncomes { get; set; } = new();
        public List<decimal> CashFlowExpenses { get; set; } = new();

        // 3. Dữ liệu cho Biểu đồ Cơ cấu Chi tiêu tháng này (Doughnut Chart)
        public List<string> ExpenseCategoryLabels { get; set; } = new();
        public List<decimal> ExpenseCategoryAmounts { get; set; } = new();
        public List<double> ExpenseCategoryPercentages { get; set; } = new();

        // 4. Danh sách giao dịch gần đây nhất (Top 5)
        public List<Transaction> RecentTransactions { get; set; } = new();

        // 5. Danh sách danh mục của User (dùng cho Modal "Thêm giao dịch nhanh")
        public List<Category> Categories { get; set; } = new();
        public double balanceGrowthRate { get; set; }
        public double incomeGrowthRate { get; set; }
    }
}
