namespace sampleapp.Models
{
    public class UpcomingExamDto
    {
        public int ExamId { get; set; }
        public string ? SubjectName  { get; set; }
        public DateTime ExamDate { get; set; }
        public string ? StartTime { get; set; }
        public string ? EndTime { get; set; }
    }

}
