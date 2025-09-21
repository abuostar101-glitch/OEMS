using Dapper;
using Microsoft.AspNetCore.Mvc;
using sampleapp.Models;
using System.Data;
using Microsoft.Data.SqlClient;

namespace OEMS.Controllers
{
    public class EmailSettingsController : Controller
    {
        private readonly string? _connectionString;

        public EmailSettingsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: EmailSettings/Index
        // Example controller code to get list of email settings
        public async Task<IActionResult> Index()
        {
            using var connection = new SqlConnection(_connectionString);

            var list = await connection.QueryAsync<EmailSettingsListItem>(
                "sp_GetEmailSettingsList", commandType: CommandType.StoredProcedure);

            return View(list.ToList());
        }


        // GET: EmailSettings/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            var setting = await connection.QueryFirstOrDefaultAsync<EmailSettingsViewModel>(
                "sp_GetEmailSettingsById",
                new { Id = id },
                commandType: CommandType.StoredProcedure);

            if (setting == null)
            {
                return NotFound();
            }

            return View(setting);
        }


        // POST: EmailSettings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EmailSettingsViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            using var connection = new SqlConnection(_connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@Id", model.Id);
            parameters.Add("@ProviderName", model.ProviderName);
            parameters.Add("@ServerName", model.ServerName);
            parameters.Add("@PortNumber", model.PortNumber);
            parameters.Add("@FromEmailId", model.FromEmailId);
            parameters.Add("@Password", model.Password);

            await connection.ExecuteAsync(
                "sp_UpdateEmailSettings", parameters, commandType: CommandType.StoredProcedure);

            TempData["SuccessMessage"] = "Email settings updated successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
