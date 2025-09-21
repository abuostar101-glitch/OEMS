using System;
using System.Collections.Generic;

namespace OEMS.Models
{
    public class CompletedExamReportViewModel
    {
        public List<CompletedExamReportModel> Exams { get; set; } = new List<CompletedExamReportModel>();

        public int TotalStudents { get; set; }
        public int TotalPass { get; set; }
        public int TotalFail { get; set; }
        public double AvgPercentage { get; set; }

        public CompletedExamReportModel TopPerformer { get; set; }
    }
}
