using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using sampleapp.Models;
using System.Data;
using System.Linq;
using System.Collections.Generic;

[Authorize(Roles = "ADMIN")]          // ← remove or change if not needed
public class UnlockController : Controller
{
    private readonly string _connectionString;

    public UnlockController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string is missing.");
    }

    // GET: /Unlock
    public IActionResult Index()
    {
        using var connection = new SqlConnection(_connectionString);
        var lockedUsers = connection.Query<UserInfo>(
            "sp_GetUnlockUserList",
            commandType: CommandType.StoredProcedure
        ).ToList();

        return View(lockedUsers);
    }

    // POST: /Unlock/UnlockUser
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UnlockUser(int userId)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Execute(
            "UPDATE Users SET IsLocked = 0, FailedLoginAttempts = 0 WHERE UserId = @UserId",
            new { UserId = userId }
        );

        TempData["SuccessMessage"] = "User unlocked successfully.";
        return RedirectToAction(nameof(Index));
    }
}
