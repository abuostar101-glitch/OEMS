using Dapper;
using System.Data;
using System.Data.SqlClient;

namespace OEMS.Models
{
    public class ReportRepository
    {
        private readonly string _connectionString;

        public ReportRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public CompletedExamReportViewModel GetCompletedExamReport()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var multi = connection.QueryMultiple("sp_GetCompletedExamReport",
                            commandType: CommandType.StoredProcedure))
                {
                    var exams = multi.Read<CompletedExamReportModel>().ToList();
                    var summary = multi.ReadSingle<dynamic>();

                    return new CompletedExamReportViewModel
                    {
                        Exams = exams,
                        TotalStudents = summary.TotalStudents,
                        TotalPass = summary.TotalPass,
                        TotalFail = summary.TotalFail,
                        AvgPercentage = summary.AvgPercentage,
                        TopPerformer = exams.FirstOrDefault(x => x.UserName == (string)summary.TopPerformer)
                    };
                }
            }
        }
    }
}
