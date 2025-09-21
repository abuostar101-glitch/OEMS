using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using sampleapp.Models;
using System.Data;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;

namespace OEMS.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _env;


        public HomeController(IConfiguration configuration, ILogger<HomeController> logger, IWebHostEnvironment env)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string is missing.");
            _logger = logger;
            _env = env;
        }


        [HttpGet]
        public IActionResult Index() => View();

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Please enter username and password.";
                return View("Index", model);
            }

            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                // Use stored procedure only — works for staff and students
                var parameters = new DynamicParameters();
                parameters.Add("@Username", model.UserName); // Can be username or reg no
                parameters.Add("@Password", model.Password);

                var validUser = connection.QueryFirstOrDefault<UserInfo>(
                    "sp_ValidateUser", parameters, commandType: CommandType.StoredProcedure);

                if (validUser == null)
                {
                    // Fetch user info (to find UserId) even if password is wrong
                    var userInfo = connection.QueryFirstOrDefault<UserInfo>(
                        @"SELECT TOP 1 u.UserId, u.FailedLoginAttempts
                  FROM Users u
                  LEFT JOIN StudentRegNoMap srm ON srm.UserId = u.UserId
                  WHERE u.UserName = @Username OR srm.RegNo = @Username",
                        new { Username = model.UserName });

                    if (userInfo != null)
                    {
                        // Increase failed attempts
                        connection.Execute(
                            "UPDATE Users SET FailedLoginAttempts = FailedLoginAttempts + 1 WHERE UserId = @UserId",
                            new { UserId = userInfo.UserId });

                        int newAttempt = userInfo.FailedLoginAttempts + 1;

                        if (newAttempt >= 3)
                        {
                            connection.Execute("UPDATE Users SET IsLocked = 1 WHERE UserId = @UserId", new { UserId = userInfo.UserId });
                            ViewBag.Error = $"Attempt {newAttempt} of 3 — Your account is now locked.";
                        }
                        else
                        {
                            ViewBag.Error = $"Attempt {newAttempt} of 3 — Invalid username or password.";
                        }
                    }
                    else
                    {
                        ViewBag.Error = "Invalid username or password.";
                    }

                    return View("Index", model);
                }

                // Reset on successful login
                connection.Execute(
                    "UPDATE Users SET FailedLoginAttempts = 0, IsLocked = 0 WHERE UserId = @UserId",
                    new { validUser.UserId });

                // Login user
                var claims = new List<Claim>
{
    new Claim(ClaimTypes.Name, validUser.UserName ?? string.Empty),
    new Claim("UserId", validUser.UserId.ToString()), // ✅ add this line
    new Claim(ClaimTypes.NameIdentifier, validUser.UserId.ToString()),
    new Claim(ClaimTypes.Role, validUser.RoleName ?? string.Empty)
};


                var identity = new ClaimsIdentity(claims, "UserCookies");
                var principal = new ClaimsPrincipal(identity);
                HttpContext.SignInAsync("UserCookies", principal).GetAwaiter().GetResult();
                HttpContext.Session.SetString("UserId", validUser.UserId.ToString());
                HttpContext.Session.SetString("UserName", validUser.UserName ?? "");
                HttpContext.Session.SetString("Role", validUser.RoleName ?? "");

                return validUser.RoleName != "STUDENT"
                    ? RedirectToAction("Welcome")
                    : RedirectToAction("Exam");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for user: {User}", model.UserName);
                ViewBag.Error = "An error occurred while logging in.";
                return View("Index", model);
            }
        }



        [Authorize]
        public IActionResult Welcome()
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var sql = "SELECT RoleId, COUNT(UserId) AS Count FROM Users GROUP BY RoleId";
            var counts = connection.Query<(int RoleId, int Count)>(sql).ToList();

            ViewBag.AdminCount = counts.FirstOrDefault(x => x.RoleId == 50).Count;
            ViewBag.StaffCount = counts.FirstOrDefault(x => x.RoleId == 41).Count;
            ViewBag.StudentCount = counts.FirstOrDefault(x => x.RoleId == 57).Count;

            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.SignOutAsync("UserCookies").GetAwaiter().GetResult();
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult ChangePassword() => View();

        [HttpPost]
        public IActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var user = connection.QueryFirstOrDefault<UserInfo>(
                "SELECT * FROM Users WHERE UserId = @UserId", new { UserId = userId });

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                return View(model);
            }

            if (user.Password != model.OldPassword)
            {
                ModelState.AddModelError(string.Empty, "Old password is incorrect.");
                return View(model);
            }

            var updateParams = new DynamicParameters();
            updateParams.Add("@UserId", userId);
            updateParams.Add("@NewPassword", model.NewPassword);

            int rows = connection.Execute("sp_UpdatePassword", updateParams, commandType: CommandType.StoredProcedure);

            if (rows > 0)
            {
                ViewBag.Message = "Password changed successfully.";
                ModelState.Clear();
                return View();
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Failed to update password.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();


        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(string.Empty, "Please enter your email.");
                return View();
            }

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var user = await connection.QueryFirstOrDefaultAsync<UserInfo>(
                "SELECT * FROM Users WHERE EmailId = @EmailId", new { EmailId = email });

            if (user == null)
            {
                TempData["ErrorMessage"] = "Email address not found.";
                return View(); 
            }


            var emailSettings = await connection.QueryFirstOrDefaultAsync<EmailSettingsViewModel>(
                "SELECT TOP 1 * FROM EmailSettings");

            if (emailSettings == null ||
                string.IsNullOrWhiteSpace(emailSettings.ServerName) ||
                string.IsNullOrWhiteSpace(emailSettings.FromEmailId) ||
                string.IsNullOrWhiteSpace(emailSettings.Password))
            {
                TempData["ErrorMessage"] = "Email settings are not configured properly. Please contact admin.";
                return View();

            }

            // ✅ Load and process the email template
            string templatePath = Path.Combine(_env.ContentRootPath, "Views", "EmailSettings", "EmailTemplate.html");

            if (!System.IO.File.Exists(templatePath))
            {
                TempData["ErrorMessage"] = "Email template not found.";
                return View();

            }

            string htmlBodyTemplate = await System.IO.File.ReadAllTextAsync(templatePath);
            string htmlBody = htmlBodyTemplate
                .Replace("{UserName}", WebUtility.HtmlEncode(user.UserName ?? ""))
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
                    Subject = "Temporary Password",
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await smtpClient.SendMailAsync(mailMessage);

                TempData["SuccessMessage"] = "A temporary password has been sent to your email.";
                return RedirectToAction("ForgotPassword"); // Ensures SweetAlert works
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error sending email: " + ex.Message;
                return RedirectToAction("ForgotPassword"); // Ensures SweetAlert works
            }




            return View();
        }
        public IActionResult Exam()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            using var connection = new SqlConnection(_connectionString);

            var sql = @"
        SELECT TOP 1 
            SubjectId,
            CASE 
                WHEN GETDATE() BETWEEN 
                     CAST(ExamDate AS DATETIME) + CAST(ExamTime AS DATETIME) AND 
                     DATEADD(MINUTE, 10, CAST(ExamDate AS DATETIME) + CAST(ExamTime AS DATETIME))
                THEN 1 
                ELSE 0 
            END AS IsWithinExamTime
        FROM StudentExam
        WHERE UserId = @UserId 
          AND ExamDate = CAST(GETDATE() AS DATE)
          AND ISNULL(IsSubmitted, 0) = 0";

            var status = connection.QueryFirstOrDefault<StudentExamStatusDto>(sql, new { UserId = userId });

            if (status != null && status.IsWithinExamTime == 1)
            {
                ViewBag.ExamLink = 1;
                ViewBag.SubjectId = status.SubjectId;
            }
            else
            {
                ViewBag.ExamLink = 0;
            }

            return View();
        }


    }
}