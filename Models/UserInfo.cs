using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;


namespace sampleapp.Models
{
    public class UserInfo
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Username is required")]
        public string? UserName { get; set; }

        [Required]
        //[EmailAddress(ErrorMessage = "Invalid email address format.")]  // Optional: can use this attribute instead of regex
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Please enter a valid email address.")]
        public  string? EmailId { get; set; }

        //[Required(ErrorMessage = "Password is required")]  // You can enable if you want to enforce password at input
        public string? Password { get; set; }

        public bool IsActive { get; set; }

        public int FailedLoginAttempts { get; set; } = 0;
        public bool IsLocked { get; set; } = false;

        // It’s fine to keep both RoleId and RoleName, but usually you only store RoleId and
        // get RoleName via join or separately if needed.
        public string? Role { get; set; }  // Consider removing this or clarifying usage

        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;

        public string? RegNo { get; set; }


        // Dropdown for Roles in your views
        public List<SelectListItem>? Roles { get; set; }
    }

    public class RoleDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }

    public class PasswordResetToken
    {
        public int UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime Expiry { get; set; }
    }

}
