using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using OEMS.Models;
using sampleapp.Models;
using System.Data;
using System.Security.Claims;

namespace OEMS.Controllers
{
    public class ExamSettingsController : Controller
    {
        private readonly IConfiguration _config;

        public ExamSettingsController(IConfiguration config)
        {
            _config = config;
        }

        public IActionResult Index()
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            var settings = connection.Query<ExamSettingViewModel>(
                "sp_GetExamSettingsList", commandType: CommandType.StoredProcedure).ToList();

            return View(settings);
        }

        public IActionResult Add()
        {
            var model = new ExamSettingViewModel
            {
                Subjects = LoadSubjects()
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult Add(ExamSettingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Subjects = LoadSubjects();
                return View(model);
            }

            try
            {
                using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                connection.Execute("sp_AddExamSetting", new
                {
                    SubjectId = model.SubjectId,
                    MaxQuestions = model.MaxQuestions,
                    MarkPerQuestion = model.MarkPerQuestion,
                    QualifyMark = model.QualifyMark,
                    DurationMinutes = model.DurationMinutes,
                    TotalMarks = model.TotalMarks,
                    CreatedBy = GetActiveUserId() // Optional, if your SP supports it
                }, commandType: CommandType.StoredProcedure);

                TempData["SuccessMessage"] = "Exam setting added successfully!";
                return RedirectToAction("Index");
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("active exam setting for this subject already exists"))
                {
                    ModelState.AddModelError(string.Empty, "This subject already has an active exam setting.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "An unexpected error occurred.");
                }

                model.Subjects = LoadSubjects();
                return View(model);
            }
        }

        public IActionResult Edit(int id)
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));

            var setting = connection.QuerySingleOrDefault<ExamSettingViewModel>(
                "sp_GetExamSettingById",
                new { Id = id },
                commandType: CommandType.StoredProcedure
            );

            if (setting == null)
                return NotFound();

            setting.Subjects = LoadSubjects(); // for dropdown
            return View("Edit", setting); // ✅ Reuse Add.cshtml
        }

        [HttpPost]
        public IActionResult Edit(ExamSettingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Subjects = LoadSubjects();
                return View("Add", model); // ✅ Return to Add view with data
            }

            try
            {
                using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));

                connection.Execute("sp_UpdateExamSetting", new
                {
                    model.Id,
                    model.SubjectId,
                    model.MaxQuestions,
                    model.MarkPerQuestion,
                    model.QualifyMark,
                    model.DurationMinutes,
                    model.TotalMarks,
                    UpdatedBy = GetActiveUserId()
                }, commandType: CommandType.StoredProcedure);

                TempData["SuccessMessage"] = "Exam setting updated successfully.";
                return RedirectToAction("Index");
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("active exam setting for this subject already exists"))
                {
                    ModelState.AddModelError(string.Empty, "This subject already has an active exam setting.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "An unexpected error occurred.");
                }

                model.Subjects = LoadSubjects();
                return View("Add", model); // ✅ Always return to the same view
            }
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            var setting = connection.QueryFirstOrDefault<ExamSettingViewModel>(
                "sp_GetExamSettingById",
                new { Id = id },
                commandType: CommandType.StoredProcedure);

            if (setting == null)
                return NotFound();

            return View(setting); // no need for Subjects unless dropdown is shown
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Execute("sp_DeleteExamSetting", new
            {
                ExamSettingId = id,
                UpdatedBy = GetActiveUserId()
            }, commandType: CommandType.StoredProcedure);

            TempData["SuccessMessage"] = "Exam setting deleted successfully.";
            return RedirectToAction("Index");
        }



        [HttpPost]
        public IActionResult SaveExamSetting(ExamSettingViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                // Repopulate subject dropdown
                viewModel.Subjects = LoadSubjects();
                return View("ExamSettingForm", viewModel);
            }

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));

            // If Id is 0 → insert
            if (viewModel.Id == 0)
            {
                conn.Execute("sp_AddExamSetting", new
                {
                    SubjectId = viewModel.SubjectId,
                    MaxQuestions = viewModel.MaxQuestions,
                    MarkPerQuestion = viewModel.MarkPerQuestion,
                    QualifyMark = viewModel.QualifyMark,
                    DurationMinutes = viewModel.DurationMinutes,
                    TotalMarks = viewModel.TotalMarks,
                    CreatedBy = GetActiveUserId()
                }, commandType: CommandType.StoredProcedure);
            }
            else
            {
                // Otherwise, it's an update
                conn.Execute("sp_UpdateExamSetting", new
                {
                    Id = viewModel.Id,
                    SubjectId = viewModel.SubjectId,
                    MaxQuestions = viewModel.MaxQuestions,
                    MarkPerQuestion = viewModel.MarkPerQuestion,
                    QualifyMark = viewModel.QualifyMark,
                    DurationMinutes = viewModel.DurationMinutes,
                    TotalMarks = viewModel.TotalMarks,
                    UpdatedBy = GetActiveUserId()
                }, commandType: CommandType.StoredProcedure);
            }

            TempData["SuccessMessage"] = "Exam setting saved successfully!";
            return RedirectToAction("Index");
        }

  


        // ✅ Dropdown source for subject list
        private List<SelectListItem> LoadSubjects()
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            var subjects = conn.Query<SubjectDto>("sp_GetAllSubjects", commandType: CommandType.StoredProcedure);

            return subjects.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.Name
            }).ToList();
        }

        // ✅ User ID from Claims
        private int GetActiveUserId()
        {
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                return userId;

            return 1; 
        }
    }
}
