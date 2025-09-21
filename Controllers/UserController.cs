using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using sampleapp.Models;
using System.Data;
using System.Security.Claims;

namespace sampleapp.Controllers
{
    [Authorize]  // Require authentication for all actions
    public class UserController : Controller
    {
        private readonly string _connectionString;

        public UserController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string is missing.");
        }

        public IActionResult Index()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var userList = connection.Query<UserInfo>(
                    "sp_GetUserList", commandType: CommandType.StoredProcedure).ToList();

                return View(userList);
            }
            catch (Exception ex)
            {
                // For example, log to console or file
                Console.WriteLine("Error fetching user list: " + ex.Message);
                ModelState.AddModelError("", "Failed to load user list.");
                return View(new List<UserInfo>());
            }

        }

        private List<SelectListItem> GetRolesList()
        {
            using var db = new SqlConnection(_connectionString);
            var roleData = db.Query<RoleDto>(
                "sp_GetRolesDDL", commandType: CommandType.StoredProcedure).ToList();

            return roleData.Select(r => new SelectListItem
            {
                Value = r.RoleId.ToString(),
                Text = r.RoleName
            }).ToList();
        }

        public IActionResult AddUser()
        {
            var userInfo = new UserInfo
            {
                Roles = GetRolesList()
            };

            return View(userInfo);
        }


        [HttpPost]
        public IActionResult AddUser(UserInfo user)
        {
            string? creatorIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(creatorIdStr, out int creatorId))
            {
                ModelState.AddModelError("", "Invalid creator user ID.");
                user.Roles = GetRolesList();
                return View(user);
            }

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var parameters = new DynamicParameters();
            parameters.Add("@UserName", user.UserName);
            parameters.Add("@EmailId", user.EmailId);
            parameters.Add("@RoleId", Convert.ToInt32(user.RoleId));
            parameters.Add("@UserId", creatorId);
            parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            //connection.Execute("sp_InsertUser", parameters, commandType: CommandType.StoredProcedure);
            try
            {
                connection.Execute("sp_InsertUser", parameters, commandType: CommandType.StoredProcedure);
                int returnValue = parameters.Get<int>("@Result");

                if (returnValue == 1)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", "Username or Email already exists.");
                }
            }
            catch (SqlException ex) when (ex.Message.Contains("UQ_User_Reg"))
            {
                ModelState.AddModelError("", "This User is already mapped to a registration number.");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred: " + ex.Message);
            }

            user.Roles = GetRolesList();
            return View(user);

        }





        public IActionResult UserEdit(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            // Get user details by id (pseudo code, adapt to your stored proc)
            var user = connection.QueryFirstOrDefault<UserInfo>("sp_GetUserById", new { UserId = id }, commandType: CommandType.StoredProcedure);

            if (user == null)
                return NotFound();

            // Get all roles for dropdown
            var roles = connection.Query<RolesInfo>("sp_GetRoleList", commandType: CommandType.StoredProcedure).ToList();

            // Pass roles list to view using ViewBag (or ViewData)
            ViewBag.Roles = new SelectList(roles, "RoleId", "RoleName", user.RoleId);
            Console.WriteLine(user.RegNo); // for debugging


            return View(user);
        }

        [HttpPost]
        public IActionResult UserEdit(UserInfo user)
        {
            string? updaterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (ModelState.IsValid)
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", user.UserId);
                parameters.Add("@UserName", user.UserName);
                parameters.Add("@EmailId", user.EmailId);
                parameters.Add("@RoleId", Convert.ToInt32(user.RoleId));
                parameters.Add("@UpdateBy", Convert.ToInt32(updaterId));

                parameters.Add("@ReturnVal", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

                connection.Execute("sp_UpdateUser", parameters, commandType: CommandType.StoredProcedure);
                int returnVal = parameters.Get<int>("@ReturnVal");

                if (returnVal == 0)
                {
                    ModelState.AddModelError("", "Username or Email already exists.");
                }
                else
                {
                    return RedirectToAction("Index");
                }
            }

            // Re-populate ViewBag.Roles to match GET action
            using var conn = new SqlConnection(_connectionString);
            var roles = conn.Query<RolesInfo>("sp_GetRoleList", commandType: CommandType.StoredProcedure).ToList();
            ViewBag.Roles = new SelectList(roles, "RoleId", "RoleName", user.RoleId);

            return View(user);
        }

        public IActionResult Delete(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", id);

            var user = connection.QueryFirstOrDefault<UserInfo>(
                "sp_GetUserById", parameters, commandType: CommandType.StoredProcedure);

            if (user == null)
                return NotFound();

            return View(user);
        }

        [HttpPost]
        public IActionResult DeleteConfirm(UserInfo userInfo)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", userInfo.UserId);

            var rowsAffected = connection.Execute(
                "sp_DeleteUser", parameters, commandType: CommandType.StoredProcedure);

            if (rowsAffected > 0)
                return RedirectToAction("Index");

            return NotFound();
        }
    }
}
