using System.ComponentModel.DataAnnotations;

namespace FinancialManagement.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [StringLength(50, ErrorMessage = "Tên đăng nhập không được vượt quá 50 ký tự")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Vui lòng nhập đúng định dạng email")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [StringLength(100)]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(
            100,
            MinimumLength = 6,
            ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự"
        )]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [DataType(DataType.Password)]
        [Compare(
            "Password",
            ErrorMessage = "Mật khẩu xác nhận không khớp"
        )]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
