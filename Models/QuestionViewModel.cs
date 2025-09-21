using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace sampleapp.Models
{
    public class QuestionViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Please select a subject.")]
        public int SubjectId { get; set; }

        public int QuestionId { get; set; } // For listing and editing

        public string? SubjectName { get; set; } // For display only, no validation needed

        [Required(ErrorMessage = "Question text is required")]
        [StringLength(500, ErrorMessage = "Question text cannot exceed 500 characters")]
        public string? QuestionText { get; set; }

        [Required(ErrorMessage = "Option A is required")]
        [StringLength(100, ErrorMessage = "Option A cannot exceed 100 characters")]
        public string? OptionA { get; set; }

        [Required(ErrorMessage = "Option B is required")]
        [StringLength(100, ErrorMessage = "Option B cannot exceed 100 characters")]
        public string? OptionB { get; set; }

        [Required(ErrorMessage = "Option C is required")]
        [StringLength(100, ErrorMessage = "Option C cannot exceed 100 characters")]
        public string? OptionC { get; set; }

        [Required(ErrorMessage = "Option D is required")]
        [StringLength(100, ErrorMessage = "Option D cannot exceed 100 characters")]
        public string? OptionD { get; set; }

        [Required(ErrorMessage = "Please select the correct answer")]
        [RegularExpression("^[ABCD]$", ErrorMessage = "Correct answer must be A, B, C or D")]
        public string? CorrectAnswer { get; set; }
    }

    public class QuestionDto
    {
        public int QuestionId { get; set; }
        public string ? QuestionText { get; set; }
        public string ? OptionA { get; set; }
        public string ? OptionB { get; set; }
        public string  ? OptionC { get; set; }
        public string ? OptionD { get; set; }
    }

    public class ExamSessionDto
    {
        public int SubjectId { get; set; }
        public List<QuestionDto> ? Questions { get; set; }
        public Dictionary<int, string> Answers { get; set; } = new();
        public int CurrentIndex { get; set; } = 0;
        public int DurationInMinutes { get; set; }

        public DateTime EndTime { get; set; }
    }


    public class StudentAnswerViewModel
    {
        public int QuestionId { get; set; }
        public string SelectedAnswer { get; set; } = string.Empty;
    }

    public class UploadQuestionsViewModel
    {
        [Required]
        [Display(Name = "Excel File")]
        public IFormFile? ExcelFile { get; set; }
    }


    public class QuizResultViewModel
    {
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int ScorePercentage { get; set; }
    }

   

}
