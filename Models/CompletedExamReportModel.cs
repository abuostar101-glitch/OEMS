using System;

namespace OEMS.Models
{
    public class CompletedExamReportModel
    {
        public int SubjectId { get; set; }
        public string RegNo { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public int Score { get; set; }
        public decimal Percentage { get; set; }
        public string Result { get; set; } = string.Empty;
    }
}
