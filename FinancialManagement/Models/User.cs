using FinancialManagement.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace FinancialManagement.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string? FullName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; }

        //Navigation Properties
        public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    }
}
