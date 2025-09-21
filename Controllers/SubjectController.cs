using AspNetCoreGeneratedDocument;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using sampleapp.Models;
using System.Data;
using System.Security.Claims;
using System.Security.Cryptography.Pkcs;


//public class SubjectController(IConfiguration configuration) : Controller

//{
//    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
//            ?? throw new InvalidOperationException("Connection string is missing.");

public class SubjectController : Controller
{
    private readonly string _connectionString;

    public SubjectController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string is missing.");
    }
    //public IActionResult Index(object subjectlist)
    //{
    //    try
    //    {
    //        using (var connection = new SqlConnection(_connectionString))
    //        {
    //            connection.Open();

    //            var Subjectlist = connection.Query<SubjectInfo>("sp_GetSubject", commandType: CommandType.StoredProcedure).ToList();

    //            if (Subjectlist.Count > 0)
    //            {
    //                return View(Subjectlist);
    //            }

    //        }
    //    }
    //    catch (Exception)
    //    {
    //    }

    //    return View(null);
    public IActionResult Index()
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                // Optional: connection.Open();

                var SubjectList = connection.Query<SubjectInfo>(
                    "sp_GetSubject",
                    commandType: CommandType.StoredProcedure
                ).ToList();

                return View("Index", SubjectList);
            }
        }
        catch (Exception)
        {
            // Log the exception ex if you have logging set up

            // Return empty list to avoid null reference errors in the view
            return View("Index", new List<SubjectInfo>());
        }
    }



    public IActionResult AddSubject()
    {
        ModelState.Clear(); // Clear any previous errors
        var newSubject = new SubjectInfo();
        return View(newSubject);
    }
    [HttpPost]
    public IActionResult AddSubject(SubjectInfo model)
    {
        if (!ModelState.IsValid)
            return View(model);

        using (var connection = new SqlConnection(_connectionString))
        {
            connection.Open();

            var existingCount = connection.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM Subjects WHERE SubjectName = @SubjectName AND IsActive = 1",
                new { model.SubjectName });

            if (existingCount > 0)
            {
                ModelState.AddModelError("", "Subject already exists!");
                return View(model);  // Return to AddSubject view with error shown
            }

            connection.Execute("sp_AddSubject", new { SubjectName = model.SubjectName, IsActive = 1 }, commandType: CommandType.StoredProcedure);

            TempData["Success"] = "Subject added successfully!";
            return RedirectToAction("Index");
        }
    }


    //[HttpPost]
    //public IActionResult AddSubject(SubjectInfo subjectInfo)
    //{
    //    if (ModelState.IsValid)
    //    {
    //        using (var connection = new SqlConnection(_connectionString))
    //        {
    //            var parameters = new DynamicParameters();
    //            parameters.Add("@SubjectName", subjectInfo.SubjectName);
    //            parameters.Add("@IsActive", subjectInfo.IsActive);
    //            parameters.Add("@RoleId", Convert.ToInt32(subjectInfo.Role));
    //            parameters.Add("@UserId", UserId);  // ← this is required!

    //            int count = connection.Execute("[dbo].[sp_InsertSubject]", parameters, commandType: CommandType.StoredProcedure);

    //            if (count == 1)
    //                return RedirectToAction("Index");
    //        }
    //    }

    //    return View(User);
    //}




    public IActionResult SubEdit(int id)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            var parameters = new DynamicParameters();
            parameters.Add("@SubjectId", id);

            var Subject = connection.QueryFirstOrDefault<SubjectInfo>("sp_GetSubjectById", parameters, commandType: CommandType.StoredProcedure);

            if (Subject == null)
                return NotFound();

            return View("subEdit", Subject); // Reusing the AddUser view for edit form
        }
    }
    [HttpPost]
    public IActionResult UpdateSubject(SubjectInfo subject)
    {
        if (!ModelState.IsValid)
            return View("SubEdit", subject);

        try
        {
            subject.IsActive = true;

            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@SubjectId", subject.SubjectId);
            parameters.Add("@SubjectName", subject.SubjectName);
            parameters.Add("@IsActive", subject.IsActive);
            parameters.Add("@ReturnVal", dbType: DbType.Int32, direction: ParameterDirection.Output);

            connection.Execute("sp_UpdateSubject", parameters, commandType: CommandType.StoredProcedure);

            int returnVal = parameters.Get<int>("@ReturnVal");

            if (returnVal == 1)
                return RedirectToAction("Index");
            else if (returnVal != 0)
                ModelState.AddModelError("", "Subject name already exists.");
            else
                ModelState.AddModelError("", "Failed to update the subject.");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error: {ex.Message}");
        }

        return View("SubEdit", subject);
    }





    public IActionResult DeleteSubject(int id)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            var parameters = new DynamicParameters();
            parameters.Add("@SubjectId", id);

            var subject = connection.QueryFirstOrDefault<SubjectInfo>("sp_GetSubjectById", parameters, commandType: CommandType.StoredProcedure);

            if (subject == null)
                return NotFound();

            return View(subject); // Create a Delete.cshtml view if needed
        }
    }

    [HttpPost]
    public IActionResult DeleteConfirmed([Bind("SubjectId,SubjectName")] SubjectInfo info)
    {
        if (info.SubjectId <= 0)
        {
            ModelState.AddModelError("", "Invalid Subject ID.");
            return View("DeleteSubject", info);
        }
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@SubjectId", info.SubjectId);

                int count = connection.Execute("sp_DeleteSubject", parameters, commandType: CommandType.StoredProcedure);
                if (count == 1)
                {
                    TempData["Success"] = "Subject deleted successfully.";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["Error"] = "Delete failed. The subject might be in use.";
                    return RedirectToAction("DeleteSubject", new { id = info.SubjectId });
                }

            }
        }
        catch (Exception)
        {
            ModelState.AddModelError("", "An error occurred while deleting the subject.");
        }

        return View("DeleteSubject", info);
    }

}



