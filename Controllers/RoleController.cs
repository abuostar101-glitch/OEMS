using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using sampleapp.Models;
using System.Data;
using System.Security.Claims;
using System.Threading.Tasks;

namespace OEMS.Controllers
{
    [Authorize]
    public class RoleController : Controller
    {
        private readonly string _connectionString;

        public RoleController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string is missing.");
        }

        // GET: /Role/Index
        public async Task<IActionResult> Index()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var roles = (await connection.QueryAsync<RolesInfo>(
                    "sp_GetRoleList", commandType: CommandType.StoredProcedure)).AsList();

                return View(roles);
            }
            catch
            {
                // log error here
                return View(null);
            }
        }

        // GET: /Role/AddRole
        public IActionResult AddRole()
        {
            return View();
        }

        // POST: /Role/AddRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRole(RolesInfo role)
        {
            string? userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                ModelState.AddModelError("", "User not identified.");
                return View(role);
            }

            if (!ModelState.IsValid)
                return View(role);

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@RoleName", role.RoleName);
                parameters.Add("@UserId", userId);
                parameters.Add("@ReturnVal", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

                await connection.ExecuteAsync("sp_InsertRole", parameters, commandType: CommandType.StoredProcedure);

                int result = parameters.Get<int>("@ReturnVal");
                if (result != 1)
                {
                    ModelState.AddModelError(nameof(role.RoleName), "Role already exists.");
                    return View(role);
                }

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                // log error
                ModelState.AddModelError("", "An unexpected error occurred.");
                return View(role);
            }
        }

        // GET: /Role/EditRole/5
        public async Task<IActionResult> EditRole(int id)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@RoleId", id);

                var role = await connection.QueryFirstOrDefaultAsync<RolesInfo>(
                    "sp_GetRoleById", parameters, commandType: CommandType.StoredProcedure);

                if (role == null)
                    return NotFound();

                return View(role);
            }
            catch
            {
                // log error
                return NotFound();
            }
        }

        // POST: /Role/EditRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(RolesInfo role)
        {
            if (!ModelState.IsValid)
                return View(role);

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@RoleId", role.RoleId);
                parameters.Add("@RoleName", role.RoleName);
                parameters.Add("@ReturnVal", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await connection.ExecuteAsync("sp_UpdateRole", parameters, commandType: CommandType.StoredProcedure);

                int returnVal = parameters.Get<int>("@ReturnVal");
                if (returnVal == 1)
                {
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError(nameof(role.RoleName), "Role name already exists.");
                    return View(role);
                }
            }
            catch
            {
                // log error
                ModelState.AddModelError("", "An unexpected error occurred.");
                return View(role);
            }
        }

        // GET: /Role/DeleteRole/5
        public async Task<IActionResult> DeleteRole(int id)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@RoleId", id);

                var role = await connection.QueryFirstOrDefaultAsync<RolesInfo>(
                    "sp_GetRoleById", parameters, commandType: CommandType.StoredProcedure);

                if (role == null)
                    return NotFound();

                return View(role);
            }
            catch
            {
                // log error
                return NotFound();
            }
        }

        // POST: /Role/DeleteConfirmed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(RolesInfo role)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@RoleId", role.RoleId);

                int count = await connection.ExecuteAsync("sp_DeleteRole", parameters, commandType: CommandType.StoredProcedure);

                if (count > 0)
                    return RedirectToAction(nameof(Index));

                ModelState.AddModelError("", "Delete failed. The role might be in use.");
                return View("DeleteRole", role);
            }
            catch
            {
                // log error
                ModelState.AddModelError("", "An unexpected error occurred.");
                return View("DeleteRole", role);
            }
        }
    }
}
