using System.ComponentModel.DataAnnotations;

namespace sampleapp.Models
{
    public class ChangePasswordViewModel
    {
        [Required]
        public int UserId { get; set; }  // Usually passed hidden from session or claims

        [Required(ErrorMessage = "Old password is required")]
        [DataType(DataType.Password)]
        public string OldPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "New password must be at least 6 characters long")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "New password and confirmation do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
