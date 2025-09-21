using System.ComponentModel.DataAnnotations;

namespace sampleapp.Models
{
    public class RolesInfo
    {
        public int RoleId { get; set; }

        [Required(ErrorMessage = "Role Name is required.")]
        public string RoleName { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}
