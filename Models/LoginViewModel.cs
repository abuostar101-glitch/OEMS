using System.ComponentModel.DataAnnotations;

namespace sampleapp.Models
{
    public class LoginViewModel
    {
        [Display(Name = "User Name")]
        [Required(ErrorMessage = "Username is required")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
    public class StudentExamStatusDto
    {
        public int SubjectId { get; set; }
        public int IsWithinExamTime { get; set; }
    }

}

