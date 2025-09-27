using OEMS.Models;
using System.Text;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using OfficeOpenXml;

namespace OEMS.Helpers
{
    public static class ReportGenerator
    {
        // ---------------- PDF ----------------
        public static byte[] GeneratePdfReport(List<CompletedExamReportModel> data, string title)
        {
            using var ms = new MemoryStream();
            var writer = new PdfWriter(ms);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf);

            // Fonts
            var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            // Title
            document.Add(new Paragraph(title)
                .SetFont(font)
                .SetFontSize(18)
                .SetFontColor(ColorConstants.BLUE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20));

            // Table with 8 columns
            var table = new Table(8).UseAllAvailableWidth();
            string[] headers = { "RegNo", "Student", "Subject", "ExamDate", "Score", "Percentage", "Result", "SubmittedAt" };

            foreach (var h in headers)
            {
                table.AddHeaderCell(new Cell().Add(new Paragraph(h).SetFont(font).SetFontColor(ColorConstants.WHITE)))
                     .SetBackgroundColor(ColorConstants.DARK_GRAY);
            }

            foreach (var exam in data)
            {
                table.AddCell(exam.RegNo);
                table.AddCell(exam.UserName);
                table.AddCell(exam.SubjectName);
                table.AddCell(exam.SubmittedAt.ToString("dd-MM-yyyy"));
                table.AddCell(exam.Score.ToString());
                table.AddCell(exam.Percentage.ToString());
                table.AddCell(exam.Result);
                table.AddCell(exam.SubmittedAt.ToString("dd-MM-yyyy HH:mm"));
            }

            document.Add(table);
            document.Close();
            return ms.ToArray();
        }

        // ---------------- EXCEL ----------------
        public static byte[] GenerateExcelReport(List<CompletedExamReportModel> data, string title)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Must set for EPPlus

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add(title);

            // Header
            string[] headers = { "RegNo", "Student", "Subject", "ExamDate", "Score", "Percentage", "Result", "SubmittedAt" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cells[1, i + 1].Value = headers[i];
                ws.Cells[1, i + 1].Style.Font.Bold = true;
            }

            // Data
            for (int i = 0; i < data.Count; i++)
            {
                var exam = data[i];
                int row = i + 2;
                ws.Cells[row, 1].Value = exam.RegNo;
                ws.Cells[row, 2].Value = exam.UserName;
                ws.Cells[row, 3].Value = exam.SubjectName;
                ws.Cells[row, 4].Value = exam.SubmittedAt.ToString("dd-MM-yyyy");
                ws.Cells[row, 5].Value = exam.Score;
                ws.Cells[row, 6].Value = exam.Percentage;
                ws.Cells[row, 7].Value = exam.Result;
                ws.Cells[row, 8].Value = exam.SubmittedAt.ToString("dd-MM-yyyy HH:mm");
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();
            return package.GetAsByteArray();
        }

        // ---------------- CSV ----------------
        public static byte[] GenerateCsvReport(List<CompletedExamReportModel> data)
        {
            var sb = new StringBuilder();
            string[] headers = { "RegNo", "Student", "Subject", "ExamDate", "Score", "Percentage", "Result", "SubmittedAt" };
            sb.AppendLine(string.Join(",", headers));

            foreach (var exam in data)
            {
                sb.AppendLine($"{exam.RegNo},{exam.UserName},{exam.SubjectName},{exam.SubmittedAt:dd-MM-yyyy},{exam.Score},{exam.Percentage},{exam.Result},{exam.SubmittedAt:dd-MM-yyyy HH:mm}");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}
