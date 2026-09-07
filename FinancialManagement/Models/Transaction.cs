using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace FinancialManagement.Models
{
    [Table("Transactions")]
    public class Transaction
    {
        [Key] [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TransactionId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        /// <summary>
        /// 'I' = Income (Khoản thu), 'E' = Expense (Khoản chi)
        /// </summary>
        [Required]
        [StringLength(1)]
        public string Type { get; set; } = "E";

        [Required(ErrorMessage = "Vui lòng nhập số tiền")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [StringLength(500, ErrorMessage = "Ghi chú không vượt quá 500 từ")]
        public string Note { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;


        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

    }
}
