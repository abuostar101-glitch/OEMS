using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace OEMS.Models
{
    public class UploadExcel
    {
        [Required(ErrorMessage = "Please upload an Excel file.")]
        [Display(Name = "Upload Excel File")]
        public IFormFile ExcelFile { get; set; } = default!;
    }
}
