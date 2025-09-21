using Dapper;
//using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OEMS.Models;
using sampleapp.Controllers; // Replace with correct namespace of SessionExtensions
using sampleapp.Models;
using System.Data;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using MyUserInfo = sampleapp.Models.UserInfo;

namespace OEMS.Controllers
{
    public class StudentExamController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly string _connectionString;

        public StudentExamController(IConfiguration config, IWebHostEnvironment env)
        {
            _config = config;
            _env = env;
            _connectionString = config.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult Create()
        {
            var vm = new StudentExamViewModel();
            PopulateDropdowns(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StudentExamViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                PopulateDropdowns(vm);
                return View(vm);
            }

            if (string.IsNullOrWhiteSpace(vm.RegistrationNumber))
            {
                ViewBag.RegNoError = "Registration number is required.";
                PopulateDropdowns(vm);
                return View(vm);
            }

            int userId = GetUserIdByRegNo(vm.RegistrationNumber);
            if (userId == 0)
            {
                ViewBag.RegNoError = "Registration number not found.";
                PopulateDropdowns(vm);
                return View(vm);
            }

            using var conn = new SqlConnection(_connectionString);
            var count = conn.ExecuteScalar<int>("sp_CheckDuplicateStudentExam", new
            {
                UserId = userId,
                SubjectId = vm.SubjectId
            });

            if (count > 0)
            {
                ViewBag.DuplicateError = "Student is already scheduled for this subject.";
                PopulateDropdowns(vm);
                return View(vm);
            }

            try
            {
                conn.Execute("sp_AddStudentExam", new
                {
                    UserId = userId,
                    SubjectId = vm.SubjectId,
                    ExamDate = vm.ExamDate ?? DateTime.Today,
                    ExamTime = vm.ExamTime ?? TimeSpan.Zero,
                    ScheduledOn = DateTime.Now,
                    CreatedBy = userId
                }, commandType: CommandType.StoredProcedure);

                var subjectName = GetAllSubjects().FirstOrDefault(s => s.Id == vm.SubjectId)?.Name ?? "Unknown";
                string timeStr = (vm.ExamTime ?? TimeSpan.Zero).ToString(@"hh\:mm");

                await SendExamEmailAsync(userId, subjectName, vm.ExamDate ?? DateTime.Today, timeStr, isReschedule: false);

                TempData["SuccessMessage"] = "Student exam scheduled successfully, and email sent.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Failed to schedule exam. Please try again.";
            }

            return RedirectToAction(nameof(List));
        }

        public IActionResult List()
        {
            using var conn = new SqlConnection(_connectionString);
            var exams = conn.Query<StudentExamListDto>("sp_GetStudentExamList", commandType: CommandType.StoredProcedure).ToList();
            ViewBag.HasExams = exams.Any();
            return View(exams);
        }

        public IActionResult Edit(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            var exam = conn.QueryFirstOrDefault<StudentExamListDto>(
                "sp_GetStudentExamById",
                new { StudentExamId = id },
                commandType: CommandType.StoredProcedure
            );

            if (exam == null) return NotFound();

            var subject = GetSubjectByName(exam.SubjectName);
            if (subject == null)
                return NotFound($"Subject '{exam.SubjectName}' not found.");

            var vm = new StudentExamViewModel
            {
                StudentExamId = exam.StudentExamId,
                RegistrationNumber = exam.RegistrationNumber,
                SubjectId = subject.Id,
                SubjectName = exam.SubjectName,
                ExamDate = exam.ExamDate ?? DateTime.Today,
                ExamTime = exam.ExamTime
            };

            PopulateDropdowns(vm);
            return View("Reschedule", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(StudentExamViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                PopulateDropdowns(vm);
                return View("Reschedule", vm);
            }

            if (!vm.ExamTime.HasValue)
            {
                ModelState.AddModelError("", "Exam time is required.");
                PopulateDropdowns(vm);
                return View("Reschedule", vm);
            }

            TimeSpan parsedTime = vm.ExamTime.Value;
            DateTime examDate = vm.ExamDate ?? DateTime.Today;
            int userId = GetUserIdByRegNo(vm.RegistrationNumber!);

            using var conn = new SqlConnection(_connectionString);
            var duplicateCount = conn.ExecuteScalar<int>(
                "sp_CheckDuplicateStudentExam",
                new
                {
                    UserId = userId,
                    SubjectId = vm.SubjectId,
                    StudentExamId = vm.StudentExamId
                },
                commandType: CommandType.StoredProcedure
            );

            if (duplicateCount > 0)
            {
                ViewBag.DuplicateError = "Student is already scheduled for this subject.";
                PopulateDropdowns(vm);
                return View("Reschedule", vm);
            }

            try
            {
                conn.Execute("sp_UpdateStudentExam", new
                {
                    StudentExamId = vm.StudentExamId,
                    SubjectId = vm.SubjectId,
                    ExamDate = examDate,
                    ExamTime = parsedTime,
                    UpdatedBy = GetActiveUserId()
                }, commandType: CommandType.StoredProcedure);

                string subjectName = GetAllSubjects().FirstOrDefault(s => s.Id == vm.SubjectId)?.Name ?? "Unknown";

                await SendExamEmailAsync(userId, subjectName, examDate, parsedTime.ToString(@"hh\:mm"), isReschedule: true);

                TempData["SuccessMessage"] = "Exam rescheduled successfully, and email sent.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Failed to reschedule exam. Please try again.";
            }

            return RedirectToAction(nameof(List));
        }
        [HttpGet]
        public IActionResult Delete(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            var exam = conn.QueryFirstOrDefault<StudentExamViewModel>(
                "sp_GetStudentExamById", new { StudentExamId = id },
                commandType: CommandType.StoredProcedure);

            if (exam == null)
                return NotFound();

            return View(exam);
        }

        // POST: StudentExam/DeleteConfirmed/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public IActionResult DeleteConfirmed(int StudentExamId)
        {
            using var conn = new SqlConnection(_connectionString);
            try
            {
                conn.Execute("sp_DeleteStudentExam", new
                {
                    StudentExamId,
                    UpdatedBy = GetActiveUserId()
                }, commandType: CommandType.StoredProcedure);

                TempData["SuccessMessage"] = "Exam deleted successfully.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Error deleting exam.";
            }

            return RedirectToAction("List");
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult Delete(int id)
        //{
        //    using var conn = new SqlConnection(_connectionString);
        //    try
        //    {
        //        conn.Execute("sp_DeleteStudentExam", new
        //        {
        //            StudentExamId = id,
        //            UpdatedBy = GetActiveUserId()
        //        }, commandType: CommandType.StoredProcedure);

        //        TempData["SuccessMessage"] = "Exam deleted successfully.";
        //    }
        //    catch
        //    {
        //        TempData["ErrorMessage"] = "Error deleting exam.";
        //    }

        //    return RedirectToAction(nameof(List));
        //}
        private int GetLoggedInUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        // ✅ ENTRY POINT TO START EXAM - GET
        public IActionResult Exam()
        {
            return View();
        }
        [HttpGet]
        public IActionResult StartExamPage(int subjectId)
        {
            using var connection = new SqlConnection(_connectionString);
            var userId = GetLoggedInUserId();

            // ❌ Already submitted check
            var alreadySubmitted = connection.ExecuteScalar<bool>(
                "SELECT ISNULL(IsSubmitted, 0) FROM StudentExam WHERE UserId = @UserId AND SubjectId = @SubjectId",
                new { UserId = userId, SubjectId = subjectId });

            if (alreadySubmitted)
            {
                TempData["ErrorMessage"] = "Already submitted.";
                return RedirectToAction("Exam", "Home");
            }

            // ✅ Scheduled Exam Time check
            var scheduled = connection.QueryFirstOrDefault<StartExamDto>(
                "sp_GetCurrentExamForStudent",
                new { UserId = userId, SubjectId = subjectId },
                commandType: CommandType.StoredProcedure);

            if (scheduled == null)
            {
                TempData["ErrorMessage"] = "Not scheduled or time is over.";
                return RedirectToAction("Exam", "Home");
            }

            // ✅ Get Exam Setting
            var setting = connection.QueryFirstOrDefault<ExamSettingDto>(
                "sp_GetExamSettingForStudent",
                new { SubjectId = subjectId },
                commandType: CommandType.StoredProcedure);

            if (setting == null)
            {
                TempData["ErrorMessage"] = "No exam setting found.";
                return RedirectToAction("Exam", "Home");
            }

            // ✅ Get Questions
            var questions = connection.Query<QuestionDto>(
                "sp_GetQuestionsBySubjectId",
                new { SubjectId = subjectId },
                commandType: CommandType.StoredProcedure
            )
            .OrderBy(q => Guid.NewGuid()) // Random
            .Take(setting.TotalQuestions ?? 10)
            .ToList();

            if (!questions.Any())
            {
                TempData["ErrorMessage"] = "No questions available.";
                return RedirectToAction("Exam", "Home");
            }

            // ✅ Prepare Exam Session
            var examSession = new ExamSessionDto
            {
                SubjectId = subjectId,
                Questions = questions,
                Answers = new Dictionary<int, string>(),
                DurationInMinutes = setting.DurationMinutes ?? setting.ExamDuration ?? 10,
                EndTime = DateTime.UtcNow.AddMinutes(setting.DurationMinutes ?? setting.ExamDuration ?? 10),
                CurrentIndex = 0
            };

            HttpContext.Session.SetObject("ExamSession", examSession);
            return View("StartExamPage", examSession);
        }


        [HttpPost]
        public async Task<IActionResult> SubmitExam(IFormCollection form)
        {
            int questionId = int.Parse(form["QuestionId"]);
            string selectedOption = form["SelectedOption"];

            var exam = HttpContext.Session.GetObject<ExamSessionDto>("ExamSession");
            if (exam == null)
            {
                TempData["ErrorMessage"] = "Session expired.";
                return RedirectToAction("Exam", "Home");
            }

            exam.Answers[questionId] = selectedOption;

            if (DateTime.UtcNow > exam.EndTime)
            {
                TempData["LogoutReason"] = "⏰ Time's up!";
                HttpContext.Session.Remove("ExamSession");
                await HttpContext.SignOutAsync();
                return RedirectToAction("ThankYou");
            }

            var userId = GetLoggedInUserId();
            using var connection = new SqlConnection(_connectionString);

            // ✅ Get exam setting again
            var setting = connection.QueryFirstOrDefault<ExamSettingDto>(
                "sp_GetExamSettingForStudent",
                new { SubjectId = exam.SubjectId },
                commandType: CommandType.StoredProcedure);

            if (setting == null)
            {
                TempData["ErrorMessage"] = "Exam setting not found.";
                return RedirectToAction("Exam", "Home");
            }

            // ✅ Get correct answers
            var correctAnswers = connection.Query<(int QuestionId, string CorrectAnswer)>(
                "SELECT QuestionId, CorrectAnswer FROM Questions WHERE SubjectId = @SubjectId AND IsActive = 1",
                new { SubjectId = exam.SubjectId }).ToDictionary(x => x.QuestionId, x => x.CorrectAnswer);

            int correctCount = 0;
            foreach (var answer in exam.Answers)
            {
                if (correctAnswers.TryGetValue(answer.Key, out var actual))
                {
                    if (string.Equals(actual, answer.Value, StringComparison.OrdinalIgnoreCase))
                        correctCount++;
                }
            }

            int markPerQ = setting.MarksPerQuestion ?? 1;
            int totalMarks = correctCount * markPerQ;
            double percentage = ((double)totalMarks / ((setting.TotalQuestions ?? 1) * markPerQ)) * 100;
            double passPercentage = setting.PassPercentage ?? 35;

            string result = percentage >= passPercentage ? "Pass" : "Fail";

            // ✅ Save result
            connection.Execute("sp_MarkExamAsSubmitted", new
            {
                UserId = userId,
                SubjectId = exam.SubjectId,
                Score = totalMarks,
                Percentage = percentage,
                Result = result
            }, commandType: CommandType.StoredProcedure);

            var subjectName = connection.QueryFirstOrDefault<string>(
            "SELECT SubjectName FROM Subjects WHERE SubjectId = @Id", new { Id = exam.SubjectId });

            await SendExamResultEmailAsync(userId, setting.SubjectName!, totalMarks, percentage, result);


            HttpContext.Session.Remove("ExamSession");
            await HttpContext.SignOutAsync();

            return RedirectToAction("ThankYou", new { userId = userId, subjectId = exam.SubjectId });

        }



        [HttpPost]
        public IActionResult Navigate(IFormCollection form)
        {
            int questionId = int.Parse(form["QuestionId"]);
            string selectedOption = form["SelectedOption"];
            string direction = form["Direction"];

            var exam = HttpContext.Session.GetObject<ExamSessionDto>("ExamSession");
            if (exam == null)
            {
                return Content("<h3 class='text-danger'>Session expired. Please login again.</h3>");
            }

            // Save answer
            exam.Answers[questionId] = selectedOption;

            // Navigate
            if (direction == "next" && exam.CurrentIndex < exam.Questions.Count - 1)
                exam.CurrentIndex++;
            else if (direction == "prev" && exam.CurrentIndex > 0)
                exam.CurrentIndex--;

            HttpContext.Session.SetObject("ExamSession", exam);
            return View("StartExamPage", exam);
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ThankYou(int userId, int subjectId)
        {
            ViewBag.UserId = userId;
            ViewBag.SubjectId = subjectId;
            return View();
        }

        public IActionResult AutoLogout(string reason = "timeout")
        {
            var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var subjectId = HttpContext.Session.GetInt32("SubjectId") ?? 0;

            TempData["LogoutReason"] = reason == "timeout"
                ? "⏰ Time's up! Your exam has been automatically submitted."
                : "✅ Your exam has been submitted successfully.";

            // Remove only exam-related session variables
            HttpContext.Session.Remove("ExamSession");

            // Redirect to ThankYou without logging out yet
            return RedirectToAction("ThankYou", new { userId, subjectId });
        }






        //public async Task<IActionResult> UpcomingExams()
        //{
        //    var studentId = HttpContext.Session.GetString("UserId");
        //    if (string.IsNullOrEmpty(studentId))
        //    {
        //        return RedirectToAction("Login", "Home");
        //    }

        //    IEnumerable<UpcomingExamDto> upcomingExams;

        //    using (var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
        //    {
        //        upcomingExams = await connection.QueryAsync<UpcomingExamDto>(
        //            "sp_GetUpcomingExamsForStudent",
        //            new { StudentId = studentId },
        //            commandType: CommandType.StoredProcedure
        //        );
        //    }

        //    return View(upcomingExams);
        //}



        private async Task SendExamEmailAsync(int userId, string subjectName, DateTime examDate, string examTime, bool isReschedule)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var user = await connection.QueryFirstOrDefaultAsync<UserInfo>(@"
    SELECT 
        u.UserId,
        u.UserName,
        u.EmailId,
        u.Password,
        srm.RegNo
    FROM Users u
    LEFT JOIN StudentRegNoMap srm ON u.UserId = srm.UserId
    WHERE u.UserId = @UserId", new { UserId = userId });


            if (user == null || string.IsNullOrWhiteSpace(user.EmailId))
                return;

            var emailSettings = await connection.QueryFirstOrDefaultAsync<EmailSettings>(
                "SELECT TOP 1 * FROM EmailSettings WHERE IsActive = 1");

            if (emailSettings == null || string.IsNullOrWhiteSpace(emailSettings.ServerName)
                || string.IsNullOrWhiteSpace(emailSettings.FromEmailId)
                || string.IsNullOrWhiteSpace(emailSettings.Password))
                return;

            string templateFile = isReschedule ? "ExamReschedule.html" : "ExamNotification.html";
            string templatePath = Path.Combine(_env.ContentRootPath, "wwwroot", "templates", templateFile);

            if (!System.IO.File.Exists(templatePath)) return;

            string htmlBodyTemplate = await System.IO.File.ReadAllTextAsync(templatePath);

            string htmlBody = htmlBodyTemplate
          .Replace("{UserName}", WebUtility.HtmlEncode(user.UserName ?? ""))
          .Replace("{Subject}", WebUtility.HtmlEncode(subjectName))
          .Replace("{ExamDate}", examDate.ToString("dd/MM/yyyy"))
          .Replace("{ExamTime}", WebUtility.HtmlEncode(examTime))
          .Replace("{RegNo}", WebUtility.HtmlEncode(user.RegNo ?? ""))
          .Replace("{Password}", WebUtility.HtmlEncode(user.Password ?? ""));
            try
            {
                using var smtpClient = new SmtpClient(emailSettings.ServerName!, emailSettings.PortNumber)
                {
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(emailSettings.FromEmailId!, emailSettings.Password!),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(emailSettings.FromEmailId!),
                    Subject = "Your Upcoming Exam Schedule",
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(user.EmailId!);

                await smtpClient.SendMailAsync(mailMessage);
            }
            catch
            {
                // Optional: log error
            }
        }

        //public IActionResult CompletedExamsReport()
        //{
        //    using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));

        //    var reportData = connection.Query<CompletedExamReportViewModel>(
        //        "sp_GetCompletedExamReport", // Or use raw query if needed
        //        commandType: CommandType.StoredProcedure
        //    ).ToList();

        //    return View(reportData);
        //}

      


        private async Task SendExamResultEmailAsync(int userId, string SubjectName, int score, double percentage, string result)
        {
            using var connection = new SqlConnection(_connectionString);

            var user = await connection.QueryFirstOrDefaultAsync<UserInfo>(
                "SELECT * FROM Users WHERE UserId = @UserId",
                new { UserId = userId }
            );
            if (user == null || string.IsNullOrWhiteSpace(user.EmailId)) return;

            var emailSettings = await connection.QueryFirstOrDefaultAsync<EmailSettings>(
                "SELECT TOP 1 * FROM EmailSettings WHERE IsActive = 1"
            );
            if (emailSettings == null ||
                string.IsNullOrWhiteSpace(emailSettings.ServerName) ||
                string.IsNullOrWhiteSpace(emailSettings.FromEmailId) ||
                string.IsNullOrWhiteSpace(emailSettings.Password))
                return;

            // Load HTML template
            string htmlTemplatePath = Path.Combine(_env.ContentRootPath, "wwwroot", "templates", "ExamResult.html");
            if (!System.IO.File.Exists(htmlTemplatePath)) return;

            string htmlContent = await System.IO.File.ReadAllTextAsync(htmlTemplatePath);

            // Replace placeholders with actual values
            string htmlBody = htmlContent
                .Replace("{UserName}", WebUtility.HtmlEncode(user.UserName ?? ""))
                .Replace("{Subject}", SubjectName)
                .Replace("{Score}", score.ToString())
                .Replace("{Percentage}", percentage.ToString("F2"))
                .Replace("{Result}", result)
                .Replace("{ExamDate}", DateTime.Now.ToString("dd/MM/yyyy"))
                .Replace("{SubmittedAt}", DateTime.Now.ToString("g"));

            // Send email without PDF
            using var smtpClient = new SmtpClient(emailSettings.ServerName!, emailSettings.PortNumber)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(emailSettings.FromEmailId!, emailSettings.Password!),
                EnableSsl = true
            };

            var mail = new MailMessage
            {
                From = new MailAddress(emailSettings.FromEmailId!),
                Subject = "🎓 Your Exam Result Summary",
                Body = htmlBody,
                IsBodyHtml = true
            };
            mail.To.Add(user.EmailId!);

            await smtpClient.SendMailAsync(mail);
        }


        private void PopulateDropdowns(StudentExamViewModel vm)
        {
            vm.Subjects = GetAllSubjects().Select(s => new SelectListItem(s.Name, s.Id.ToString())).ToList();
            vm.RegNos = GetStudents().Select(s => new SelectListItem($"{s.RegNo} - {s.UserName}", s.RegNo)).ToList();
        }

        private int GetUserIdByRegNo(string regNo)
        {
            using var conn = new SqlConnection(_connectionString);
            return conn.QueryFirstOrDefault<int>("sp_GetUserIdByRegNo", new { RegNo = regNo }, commandType: CommandType.StoredProcedure);
        }

        private List<Subject> GetAllSubjects()
        {
            using var conn = new SqlConnection(_connectionString);
            return conn.Query<Subject>("sp_GetAllSubjects", commandType: CommandType.StoredProcedure).ToList();
        }

        private Subject? GetSubjectByName(string name)
        {
            using var conn = new SqlConnection(_connectionString);
            return conn.QueryFirstOrDefault<Subject>("sp_GetSubjectByName", new { Name = name }, commandType: CommandType.StoredProcedure);
        }

        private List<UserInfo> GetStudents()
        {
            using var conn = new SqlConnection(_connectionString);
            return conn.Query<UserInfo>("sp_GetUserList", commandType: CommandType.StoredProcedure)
                .Where(u => u.Role == "STUDENT").ToList();
        }

        private int GetActiveUserId()
        {
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                return userId;
            return 1;
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login", "Home");
        }

    }
}
