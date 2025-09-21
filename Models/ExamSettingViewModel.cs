using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace sampleapp.Models
{
    public class ExamSettingViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Subject is required")]
        public int? SubjectId { get; set; }

        public string? SubjectName { get; set; }

        public List<SelectListItem>? Subjects { get; set; }  // 🔥 Add this

        [Required(ErrorMessage = "Max questions is required")]
        public int? MaxQuestions { get; set; }

        [Required(ErrorMessage = "Mark per question is required")]
        public int? MarkPerQuestion { get; set; }

        [Required(ErrorMessage = "Qualify mark is required")]
        public int? QualifyMark { get; set; }

        [Required(ErrorMessage = "Duration is required")]
        public int? DurationMinutes { get; set; }

        [Required(ErrorMessage = "Total marks is required")]
        public int? TotalMarks { get; set; }

    

    }
    public class SubjectDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
    public class ExamSettingDto
    {
        public int SubjectId { get; set; }

        public string? SubjectName { get; set; }

        [Required]
        public int? TotalQuestions { get; set; }   // 👈 This is the same as MaxQuestions

        [Required]
        public int? MarksPerQuestion { get; set; }

        [Required]
        public double? PassPercentage { get; set; }

        [Required]
        public int? ExamDuration { get; set; }     // 👈 This is the same as DurationMinutes

        public int? TotalMarks { get; set; }       // Optional if you're using it for result display

        // 👇 ADD THESE ALIASES to match with code references
        public int? MaxQuestions => TotalQuestions;

        public int? DurationMinutes => ExamDuration;
    }





}
