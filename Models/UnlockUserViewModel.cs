using Microsoft.AspNetCore.Mvc;

namespace sampleapp.Models
{
    public class UnlockUserViewModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public string EmailId { get; set; } = string.Empty;



    }

}