using System.ComponentModel.DataAnnotations;

namespace sampleapp.Models
{
    public class EmailSettingsViewModel
    {
        public int Id { get; set; }

        [Required]
        public string? ProviderName { get; set; }


        [Required]
        [Display(Name = "SMTP Server Name")]
        public string ?ServerName { get; set; }

        [Required]
        [Display(Name = "Port Number")]
        public int PortNumber { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "From Email ID")]
        public string ?FromEmailId { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string?Password { get; set; }
    }


    public class EmailSettingsListItem
    {
        public int Id { get; set; }
        public string? ServerName { get; set; }
        public int PortNumber { get; set; }
        public string? FromEmailId { get; set; }
    }
}
