namespace OEMS.Models
{
    public class StudentExamResultViewModel
    {
        public int StudentExamId { get; set; }
        public string ? SubjectName { get; set; }
        public int Score { get; set; }
        public double Percentage { get; set; }
        public string ? Result { get; set; }
        public DateTime ExamDate { get; set; }
        public TimeSpan ExamTime { get; set; }
        public DateTime? SubmittedAt { get; set; }
    }


}
