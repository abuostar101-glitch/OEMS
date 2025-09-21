using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OEMS.Models;
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
