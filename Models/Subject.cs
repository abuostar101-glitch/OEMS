using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace sampleapp.Models
{
    public class SubjectInfo
    {
        public int SubjectId { get; set; }

        [Required(ErrorMessage = "Subject Name is required.")]
        [StringLength(100, ErrorMessage = "Subject Name cannot be longer than 100 characters.")]
        public string SubjectName { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public string? Role { get; set; } // Role name (optional)

        public List<SelectListItem>? Roles { get; set; }
    }

}
