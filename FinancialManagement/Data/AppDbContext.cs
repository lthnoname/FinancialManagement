using Microsoft.EntityFrameworkCore;
using FinancialManagement.Models;
namespace FinancialManagement.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Cấu hình Users
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            //2. Cấu hình Categories
            // Unique constraint: Mỗi user không được trùng tên danh mục cùng loại (Thu/Chi)
            modelBuilder.Entity<Category>()
                .HasIndex(c => new { c.UserId, c.CategoryName, c.Type })
                .IsUnique();

            //Quan hệ User - Categories (Xóa User sẽ xóa hết Category của User đó)
            modelBuilder.Entity<Category>()
                .HasOne(c => c.User)
                .WithMany(u => u.Categories)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 3. Cấu hình Transactions
            // Quan hệ User - Transactions (Xóa User sẽ xóa hết Transaction của User đó)
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.User)
                .WithMany(u => u.Transactions)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quan hệ Category - Transactions (Tránh lỗi Multiple Cascade Paths trong SQL Server)
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình định dạng tiền tệ cho Amount
            modelBuilder.Entity<Transaction>()
                .Property(t => t.Amount)
                .HasPrecision(18, 2);

        }

    }
}
