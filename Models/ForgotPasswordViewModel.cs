using System.ComponentModel.DataAnnotations;

namespace sampleapp.Models
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }

    }
}
