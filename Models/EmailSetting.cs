namespace OEMS.Models
{
    public class EmailSettings
    {
        public int Id { get; set; }
        public string? ProviderName { get; set; }
        public string? ServerName { get; set; }
        public int PortNumber { get; set; }
        public string? FromEmailId { get; set; }
        public string? Password { get; set; }
        public bool IsActive { get; set; }
    }
}
