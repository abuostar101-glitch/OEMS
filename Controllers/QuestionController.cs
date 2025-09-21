using ClosedXML.Excel;
using Dapper;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using OEMS.Models;
using sampleapp.Models;
using System.Data;
using System.Security.Claims;

namespace OEMS.Controllers
{
    public class QuestionController : Controller
    {
        private readonly IConfiguration _config;

        public QuestionController(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        private List<SubjectInfo> GetSubjects()
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            return connection.Query<SubjectInfo>("SELECT SubjectId, SubjectName FROM Subjects WHERE IsActive = 1").ToList();
        }

        public IActionResult List(int? subjectId)
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));

            var parameters = new DynamicParameters();
            parameters.Add("@SubjectId", subjectId);

            var questions = connection.Query<QuestionViewModel>(
                "sp_GetActiveQuestionsBySubject",
                parameters,
                commandType: CommandType.StoredProcedure
            ).ToList();

            ViewBag.Subjects = GetSubjects();
            ViewBag.SelectedSubjectId = subjectId;

            return View(questions);
        }

        private void LoadSubjects()
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            connection.Open();

            var subjects = connection.Query<SubjectInfo>(
                "sp_GetActiveSubjects",
                commandType: CommandType.StoredProcedure).ToList();

            ViewBag.Subjects = new SelectList(subjects, "SubjectId", "SubjectName");
        }

        [HttpGet]
        public IActionResult Add()
        {
            LoadSubjects();
            return View();
        }

        [HttpPost]
        public IActionResult Add(QuestionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                LoadSubjects();
                return View(model);
            }

            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            connection.Open();

            var existing = connection.QueryFirstOrDefault<QuestionViewModel>(
                "SELECT * FROM Questions WHERE SubjectId = @SubjectId AND QuestionText = @QuestionText",
                new { model.SubjectId, model.QuestionText });

            if (existing != null)
            {
                TempData["ErrorMessage"] = "This question already exists for the selected subject.";
                LoadSubjects();
                return View(model);
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                TempData["ErrorMessage"] = "User is not logged in.";
                LoadSubjects();
                return View(model);
            }

            int userId = int.Parse(userIdClaim);


            var parameters = new DynamicParameters();
            parameters.Add("@SubjectId", model.SubjectId);
            parameters.Add("@QuestionText", model.QuestionText);
            parameters.Add("@OptionA", model.OptionA);
            parameters.Add("@OptionB", model.OptionB);
            parameters.Add("@OptionC", model.OptionC);
            parameters.Add("@OptionD", model.OptionD);
            parameters.Add("@CorrectAnswer", model.CorrectAnswer);
            parameters.Add("@CreatedBy", userId);
            parameters.Add("@CreatedOn", DateTime.Now);

            connection.Execute("sp_AddQuestion", parameters, commandType: CommandType.StoredProcedure);

            TempData["SuccessMessage"] = "Question added successfully!";
            return RedirectToAction("Add");
        }

        [HttpGet]
        [Route("Question/Edit/{id}")]
        public IActionResult Edit(int id)
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            connection.Open();

            var question = connection.QueryFirstOrDefault<QuestionViewModel>(
                "sp_GetQuestionById",
                new { QuestionId = id },
                commandType: CommandType.StoredProcedure);

            if (question == null)
                return NotFound();

            LoadSubjects();
            return View(question);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(QuestionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                LoadSubjects();
                return View(model);
            }
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                TempData["ErrorMessage"] = "Unable to determine logged-in user ID.";
                return RedirectToAction("Add"); // or wherever appropriate
            }

            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            connection.Open();

            var parameters = new DynamicParameters();
            parameters.Add("@QuestionId", model.QuestionId);
            parameters.Add("@SubjectId", model.SubjectId);
            parameters.Add("@QuestionText", model.QuestionText);
            parameters.Add("@OptionA", model.OptionA);
            parameters.Add("@OptionB", model.OptionB);
            parameters.Add("@OptionC", model.OptionC);
            parameters.Add("@OptionD", model.OptionD);
            parameters.Add("@CorrectAnswer", model.CorrectAnswer);
            parameters.Add("@UpdatedBy", userId); // Correct: integer value
            parameters.Add("@UpdatedOn", DateTime.Now);

            connection.Execute("sp_UpdateQuestion", parameters, commandType: CommandType.StoredProcedure);

            TempData["SuccessMessage"] = "Question updated successfully!";
            return RedirectToAction("List");
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            connection.Open();

            var parameters = new DynamicParameters();
            parameters.Add("@QuestionId", id);

            var question = connection.QueryFirstOrDefault<QuestionViewModel>(
                "sp_GetQuestionById",
                parameters,
                commandType: CommandType.StoredProcedure);

            if (question == null)
                return NotFound();

            return View(question);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            connection.Open();

            var parameters = new DynamicParameters();
            parameters.Add("@QuestionId", id);
            parameters.Add("@UpdatedBy", GetActiveUserId());
            parameters.Add("@UpdatedOn", DateTime.Now);

            connection.Execute("sp_DeleteQuestion", parameters, commandType: CommandType.StoredProcedure);

            TempData["SuccessMessage"] = "Question deleted successfully!";
            return RedirectToAction("List");
        }



        [HttpGet]
        public IActionResult UploadQuestions()
        {
            return View(new UploadQuestionsViewModel());
        }

        //[HttpPost]
        //public async Task<IActionResult> UploadQuestions(UploadQuestionsViewModel model)
        //{
        //    if (model.ExcelFile == null || model.ExcelFile.Length == 0)
        //    {
        //        ModelState.AddModelError(string.Empty, "Please upload a valid Excel file.");
        //        return View(model);
        //    }

        //    // Load Excel file into memory
        //    using var stream = new MemoryStream();
        //    await model.ExcelFile.CopyToAsync(stream);

        //    // Set EPPlus license context (for non-commercial use)
        //    #pragma warning disable CS0618 // Suppress obsolete warning
        //    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        //     #pragma warning restore CS0618

        //    using var package = new ExcelPackage(stream);
        //    var worksheet = package.Workbook.Worksheets.FirstOrDefault();

        //    if (worksheet == null || worksheet.Dimension == null)
        //    {
        //        ModelState.AddModelError(string.Empty, "The uploaded Excel file does not contain a valid worksheet.");
        //        return View(model);
        //    }

        //    int rowCount = worksheet.Dimension.Rows;

        //    using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
        //    connection.Open();

        //    for (int row = 2; row <= rowCount; row++)
        //    {
        //        string subjectName = worksheet.Cells[row, 1].Text.Trim();
        //        string questionText = worksheet.Cells[row, 2].Text.Trim();
        //        string optionA = worksheet.Cells[row, 3].Text.Trim();
        //        string optionB = worksheet.Cells[row, 4].Text.Trim();
        //        string optionC = worksheet.Cells[row, 5].Text.Trim();
        //        string optionD = worksheet.Cells[row, 6].Text.Trim();
        //        string correctAnswer = worksheet.Cells[row, 7].Text.Trim().ToUpper();

        //        // Fetch subject ID from the database
        //        int subjectId = connection.QueryFirstOrDefault<int>(
        //            "SELECT SubjectId FROM Subjects WHERE SubjectName = @Name AND IsActive = 1",
        //            new { Name = subjectName });

        //        if (subjectId == 0)
        //            continue; // Skip if subject not found

        //        // Insert question into database
        //        connection.Execute("sp_AddQuestion", new
        //        {
        //            SubjectId = subjectId,
        //            QuestionText = questionText,
        //            OptionA = optionA,
        //            OptionB = optionB,
        //            OptionC = optionC,
        //            OptionD = optionD,
        //            CorrectAnswer = correctAnswer,
        //            CreatedBy = User.Identity?.Name ?? "admin",
        //            CreatedOn = DateTime.Now
        //        }, commandType: CommandType.StoredProcedure);
        //    }

        //    TempData["Success"] = "Questions uploaded successfully!";
        //    return RedirectToAction("List");
        //}

        //public IActionResult DownloadSampleExcel()
        //{
        //    var stream = new MemoryStream();

        //    #pragma warning disable CS0618 // Suppress obsolete warning
        //    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        //    #pragma warning restore CS0618
        //    // EPPlus 8+
        //    using (var package = new ExcelPackage(stream))
        //    {
        //        var worksheet = package.Workbook.Worksheets.Add("SampleQuestions");

        //        // Add headers
        //        worksheet.Cells[1, 1].Value = "Subject";
        //        worksheet.Cells[1, 2].Value = "Question Text";
        //        worksheet.Cells[1, 3].Value = "OptionA";
        //        worksheet.Cells[1, 4].Value = "OptionB";
        //        worksheet.Cells[1, 5].Value = "OptionC";
        //        worksheet.Cells[1, 6].Value = "OptionD";
        //        worksheet.Cells[1, 7].Value = "CorrectAnswer";

        //        // Sample row
        //        worksheet.Cells[2, 1].Value = "PYTHON";
        //        worksheet.Cells[2, 2].Value = "What is a list?";
        //        worksheet.Cells[2, 3].Value = "Ordered collection";
        //        worksheet.Cells[2, 4].Value = "Unordered collection";
        //        worksheet.Cells[2, 5].Value = "Single value";
        //        worksheet.Cells[2, 6].Value = "None of these";
        //        worksheet.Cells[2, 7].Value = "A";

        //        package.Save();
        //    }

        //    stream.Position = 0;
        //    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SampleQuestions.xlsx");
        //}

        //public IActionResult ExportQuestions(int? subjectId)
        //{
        //    using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
        //    connection.Open();

        //    var parameters = new DynamicParameters();
        //    parameters.Add("@SubjectId", subjectId);

        //    var questions = connection.Query<QuestionViewModel>(
        //        "sp_GetActiveQuestionsBySubject",
        //        parameters,
        //        commandType: CommandType.StoredProcedure).ToList();

        //    using var package = new ExcelPackage();
        //    var worksheet = package.Workbook.Worksheets.Add("Questions");

        //    // Headers
        //    worksheet.Cells[1, 1].Value = "Subject";
        //    worksheet.Cells[1, 2].Value = "Question Text";
        //    worksheet.Cells[1, 3].Value = "Option A";
        //    worksheet.Cells[1, 4].Value = "Option B";
        //    worksheet.Cells[1, 5].Value = "Option C";
        //    worksheet.Cells[1, 6].Value = "Option D";
        //    worksheet.Cells[1, 7].Value = "Correct Answer";

        //    int row = 2;
        //    foreach (var q in questions)
        //    {
        //        worksheet.Cells[row, 1].Value = q.SubjectName;
        //        worksheet.Cells[row, 2].Value = q.QuestionText;
        //        worksheet.Cells[row, 3].Value = q.OptionA;
        //        worksheet.Cells[row, 4].Value = q.OptionB;
        //        worksheet.Cells[row, 5].Value = q.OptionC;
        //        worksheet.Cells[row, 6].Value = q.OptionD;
        //        worksheet.Cells[row, 7].Value = q.CorrectAnswer;
        //        row++;
        //    }

        //    var stream = new MemoryStream();
        //    package.SaveAs(stream);
        //    stream.Position = 0;

        //    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ExportedQuestions.xlsx");
        //}





        [Authorize(Roles = "STUDENT")]
        [HttpGet]
        public IActionResult StartQuiz(int subjectId)
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            connection.Open();

            var questions = connection.Query<QuestionViewModel>(
                "sp_GetActiveQuestionsBySubject",
                new { SubjectId = subjectId },
                commandType: CommandType.StoredProcedure).ToList();

            return View(questions);
        }



        [HttpPost]
        public IActionResult SubmitQuiz(List<StudentAnswerViewModel> answers)
        {
            if (answers == null || answers.Count == 0)
            {
                TempData["ErrorMessage"] = "No answers submitted.";
                return RedirectToAction("StartQuiz");
            }

            int correct = 0;
            int total = answers.Count;

            using (var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                connection.Open();

                foreach (var answer in answers)
                {
                    var correctAnswer = connection.QueryFirstOrDefault<string>(
                        "SELECT CorrectAnswer FROM Questions WHERE QuestionId = @QuestionId",
                        new { QuestionId = answer.QuestionId });

                    if (!string.IsNullOrEmpty(correctAnswer) && correctAnswer == answer.SelectedAnswer)
                        correct++;
                }

                int userId = GetCurrentUserId();

                connection.Execute(
                    "sp_SaveQuizAttempt",
                    new
                    {
                        UserId = userId,
                        Score = correct,
                        TotalQuestions = total
                    },
                    commandType: CommandType.StoredProcedure);
            }

            TempData["SuccessMessage"] = $"Quiz submitted. You scored {correct} out of {total}.";
            return RedirectToAction("StartQuiz");
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }
        public IActionResult BulkUpload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(UploadExcel model)
        {
            // 1️⃣ Basic model / file checks
            if (!ModelState.IsValid)
                return View("BulkUpload", model);

            var file = model.ExcelFile;
            if (Path.GetExtension(file.FileName).ToLower() != ".xlsx")
            {
                ModelState.AddModelError("ExcelFile", "Only .xlsx files are allowed.");
                return View("BulkUpload", model);
            }

            // 2️⃣ Build DataTable from Excel
            var dt = new DataTable();
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);

                var headerRow = worksheet.FirstRowUsed();
                if (headerRow == null)
                {
                    TempData["ErrorMessage"] = "The uploaded Excel sheet is empty.";
                    return RedirectToAction("BulkUpload");
                }

                foreach (var cell in headerRow.Cells())
                    dt.Columns.Add(cell.GetString());

                string[] requiredCols = { "Subject", "Question", "OptionA", "OptionB",
                                  "OptionC", "OptionD", "CorrectAnswer" };

                foreach (var col in requiredCols)
                {
                    if (!dt.Columns.Contains(col))
                    {
                        TempData["ErrorMessage"] = $"Missing required column: {col}";
                        return RedirectToAction("BulkUpload");
                    }
                }

                foreach (var dataRow in worksheet.RowsUsed().Skip(1))
                {
                    var newRow = dt.NewRow();
                    for (int i = 0; i < dt.Columns.Count; i++)
                        newRow[i] = dataRow.Cell(i + 1).GetString();
                    dt.Rows.Add(newRow);
                }
            }

            if (dt.Rows.Count == 0)
            {
                TempData["ErrorMessage"] = "The uploaded file does not contain any data.";
                return RedirectToAction("BulkUpload");
            }

            // 3️⃣ Remove duplicates *inside Excel* (Subject + Question)
            var duplicateGroups = dt.AsEnumerable()
                .GroupBy(r => new {
                    Subject = r.Field<string>("Subject")?.Trim().ToUpper(),
                    Question = r.Field<string>("Question")?.Trim().ToUpper()
                })
                .Where(g => g.Count() > 1)
                .ToList();

            int excelDupes = 0;
            foreach (var grp in duplicateGroups)
            {
                // Keep the first row, remove the rest
                foreach (var dupRow in grp.Skip(1))
                {
                    dt.Rows.Remove(dupRow);
                    excelDupes++;
                }
            }

            // 4️⃣ Get userId from claims
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                TempData["ErrorMessage"] = "User is not logged in.";
                return RedirectToAction("BulkUpload");
            }
            int userId = int.Parse(userIdClaim);

            // 5️⃣ Send TVP to stored proc (which skips DB duplicates via NOT EXISTS)
            int insertedRows;
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();

            using var cmd = new SqlCommand("sp_InsertBulkUpload", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.Add(new SqlParameter
            {
                ParameterName = "@ExamData",
                SqlDbType = SqlDbType.Structured,
                TypeName = "dbo.QuestionType",
                Value = dt
            });
            cmd.Parameters.AddWithValue("@userId", userId);

            insertedRows = await cmd.ExecuteNonQueryAsync();   // returns rows affected

            // 6️⃣ Prepare summary
            int skippedInExcel = excelDupes;
            int attempted = dt.Rows.Count + skippedInExcel;
            int skippedInDB = attempted - insertedRows - skippedInExcel; // caught by NOT EXISTS / unique index

            TempData["SuccessMessage"] =
                $"Added: {insertedRows}, Skipped (Excel duplicates): {skippedInExcel}, " +
                $"Skipped (already in DB): {skippedInDB}";

            return RedirectToAction("BulkUpload");
        }

        private int GetActiveUserId()
        {
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                return userId;

            return 1; // fallback user ID (you can change this if needed)
        }



    }


}

