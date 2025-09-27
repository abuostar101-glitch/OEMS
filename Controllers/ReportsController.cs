using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OEMS.Helpers;
using OEMS.Models;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using Org.BouncyCastle.Bcpg.Sig;
using System.Data;
namespace OEMS.Controllers
{
    [Authorize(Roles = "ADMIN,STAFF")]
    public class ReportsController : Controller
    {
        private readonly IConfiguration _config;

        public ReportsController(IConfiguration config)
        {
            _config = config;
        }

        public async Task<IActionResult> CompletedExamReport(int? subjectId, string startDate, string endDate)
        {
            var data = await GetCompletedExamReportAsync();

            DateTime? start = null;
            DateTime? end = null;

            if (!string.IsNullOrEmpty(startDate))
                start = DateTime.ParseExact(startDate, "dd/MM/yyyy", null);

            if (!string.IsNullOrEmpty(endDate))
                end = DateTime.ParseExact(endDate, "dd/MM/yyyy", null);

            // Apply filters
            if (subjectId.HasValue)
                data = data.Where(x => x.SubjectId == subjectId.Value).ToList();

            if (start.HasValue)
                data = data.Where(x => x.SubmittedAt.Date >= start.Value.Date).ToList();

            if (end.HasValue)
                data = data.Where(x => x.SubmittedAt.Date <= end.Value.Date).ToList();

            // Build ViewModel
            var viewModel = new CompletedExamReportViewModel
            {
                Exams = data,
                TotalStudents = data.Count,
                TotalPass = data.Count(x => x.Result.Trim().ToUpper() == "PASS"),
                TotalFail = data.Count(x => x.Result.Trim().ToUpper() == "FAIL"),
                AvgPercentage = data.Any() ? Math.Round((double)data.Average(x => x.Percentage), 2) : 0,
                TopPerformer = data.OrderByDescending(x => x.Percentage).FirstOrDefault()
            };


            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            ViewBag.Subjects = connection.Query<SubjectModel>("sp_GetSubjects", commandType: CommandType.StoredProcedure).ToList();

            ViewBag.SubjectId = subjectId;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            return View(viewModel);
        }




        public IActionResult DownloadExamReportPdf(int? subjectId, string startDate, string endDate)
        {
            var data = ApplyFilters(subjectId, startDate, endDate); // Method to get filtered data
            var bytes = ReportGenerator.GeneratePdfReport(data, "Completed Exam Report");
            return File(bytes, "application/pdf", "CompletedExamReport.pdf");
        }

        public IActionResult DownloadExamReport(int? subjectId, string startDate, string endDate)
        {
            var data = ApplyFilters(subjectId, startDate, endDate);
            var bytes = ReportGenerator.GenerateExcelReport(data, "Completed Exam Report");
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "CompletedExamReport.xlsx");
        }

        public IActionResult DownloadExamReportCsv(int? subjectId, string startDate, string endDate)
        {
            var data = ApplyFilters(subjectId, startDate, endDate);
            var bytes = ReportGenerator.GenerateCsvReport(data);
            return File(bytes, "text/csv", "CompletedExamReport.csv");
        }

        // Helper to apply the same filters
        private List<CompletedExamReportModel> ApplyFilters(int? subjectId, string startDate, string endDate)
        {
            var data = GetCompletedExamReportAsync().Result;

            DateTime? start = string.IsNullOrEmpty(startDate) ? null : DateTime.ParseExact(startDate, "dd/MM/yyyy", null);
            DateTime? end = string.IsNullOrEmpty(endDate) ? null : DateTime.ParseExact(endDate, "dd/MM/yyyy", null);

            if (subjectId.HasValue)
                data = data.Where(x => x.SubjectId == subjectId.Value).ToList();
            if (start.HasValue)
                data = data.Where(x => x.SubmittedAt.Date >= start.Value.Date).ToList();
            if (end.HasValue)
                data = data.Where(x => x.SubmittedAt.Date <= end.Value.Date).ToList();

            return data;
        }
      private async Task<List<CompletedExamReportModel>> GetCompletedExamReportAsync()
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            var data = await connection.QueryAsync<CompletedExamReportModel>(
                "sp_GetCompletedExamReport",
                commandType: CommandType.StoredProcedure
            );
            return data.ToList();
        }
    }
}
