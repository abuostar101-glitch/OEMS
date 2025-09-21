using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace OEMS.Models
{
    public class StudentExamViewModel
    {
        public int StudentExamId { get; set; }

        [Required(ErrorMessage = "Registration number is required")]
        public string? RegistrationNumber { get; set; }

        [Required(ErrorMessage = "Subject is required")]
        public int SubjectId { get; set; }

        public string? SubjectName { get; set; }

        [Required(ErrorMessage = "Exam date is required")]
        [Display(Name = "Exam Date")]
        [DataType(DataType.Date)]
        public DateTime? ExamDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Exam time is required")]
        [Display(Name = "Exam Time")]
        [DataType(DataType.Time)]
        public TimeSpan? ExamTime { get; set; }

        public IEnumerable<SelectListItem>? Subjects { get; set; }

        public IEnumerable<SelectListItem>? RegNos { get; set; }
    }

    public class StudentExamListDto
    {
        public int StudentExamId { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? StudentName { get; set; }

        public string SubjectName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Exam date is required")]
        [DataType(DataType.Date)]
        public DateTime? ExamDate { get; set; }

        [Required(ErrorMessage = "Exam time is required")]
        [DataType(DataType.Time)]
        public TimeSpan? ExamTime { get; set; }

        public bool IsActive { get; set; }
    }

    public class StartExamDto
    {
        public DateTime ExamDate { get; set; }
        public TimeSpan ExamTime { get; set; }
        public string ? SubjectName { get; set; }
        public int SubjectId { get; set; }
    }


    public class Subject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Subject name is required")]
        public string? Name { get; set; }
    }
}

