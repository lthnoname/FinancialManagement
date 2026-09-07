using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialManagement.Models
{
    [Table("Categories")]
    public class Category
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CategoryId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên danh mục")]
        [StringLength(100, ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự")]
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>
        /// 'I' = Income (Khoản thu), 'E' = Expense (Khoản chi)
        /// </summary>
        [Required]
        [StringLength(1)]
        public string Type { get; set; } = "E";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        //Navigation property
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        public  virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
