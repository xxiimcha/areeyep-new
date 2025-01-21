using Microsoft.AspNetCore.Mvc;
using AreEyeP.Data;
using System.Linq;
using System.Text;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace AreEyeP.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Application Status Data
            var applicationStatuses = _context.BurialApplications
                .GroupBy(a => a.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToList();

            var applicationStatusLabels = applicationStatuses.Select(s => s.Status).ToList();
            var applicationStatusData = applicationStatuses.Select(s => s.Count).ToList();

            // Service Requests Data (Include all services even if no requests)
            var allServices = _context.Services.Select(s => new { s.Id, s.ServiceName }).ToList();

            var serviceRequestCounts = _context.ServiceRequests
                .GroupBy(sr => sr.ServiceType)
                .Select(g => new
                {
                    ServiceId = g.Key,
                    Count = g.Count()
                })
                .ToList();

            var servicesReport = allServices.Select(service => new
            {
                ServiceName = service.ServiceName,
                Count = serviceRequestCounts
                    .FirstOrDefault(sr => sr.ServiceId == service.Id.ToString())
                    ?.Count ?? 0
            }).ToList();

            var serviceRequestLabels = servicesReport.Select(s => s.ServiceName).ToList();
            var serviceRequestData = servicesReport.Select(s => s.Count).ToList();

            // Payments Report Data
            var paymentsReport = _context.ClientPayments
                .GroupBy(p => p.ServiceType)
                .Select(g => new
                {
                    ServiceType = g.Key,
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .ToList();

            var paymentLabels = paymentsReport.Select(p => p.ServiceType).ToList();
            var paymentData = paymentsReport.Select(p => p.TotalAmount).ToList();

            // Pass serialized JSON strings to the view
            ViewData["ApplicationStatusLabels"] = System.Text.Json.JsonSerializer.Serialize(applicationStatusLabels);
            ViewData["ApplicationStatusData"] = System.Text.Json.JsonSerializer.Serialize(applicationStatusData);
            ViewData["ServiceRequestLabels"] = System.Text.Json.JsonSerializer.Serialize(serviceRequestLabels);
            ViewData["ServiceRequestData"] = System.Text.Json.JsonSerializer.Serialize(serviceRequestData);
            ViewData["PaymentLabels"] = System.Text.Json.JsonSerializer.Serialize(paymentLabels);
            ViewData["PaymentData"] = System.Text.Json.JsonSerializer.Serialize(paymentData);

            return View("~/Views/LGU/Reports.cshtml");
        }

        public IActionResult DownloadReport()
        {
            // Application Status Data
            var applicationStatuses = _context.BurialApplications
                .GroupBy(a => a.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToList();

            // Service Requests Data (Include all services even if no requests)
            var allServices = _context.Services.Select(s => new { s.Id, s.ServiceName }).ToList();

            var serviceRequestCounts = _context.ServiceRequests
                .GroupBy(sr => sr.ServiceType)
                .Select(g => new
                {
                    ServiceId = g.Key,
                    Count = g.Count()
                })
                .ToList();

            var servicesReport = allServices.Select(service => new
            {
                ServiceName = service.ServiceName,
                Count = serviceRequestCounts
                    .FirstOrDefault(sr => sr.ServiceId == service.Id.ToString())
                    ?.Count ?? 0
            }).ToList();

            // Payments Report Data
            var paymentsReport = _context.ClientPayments
                .GroupBy(p => p.ServiceType)
                .Select(g => new
                {
                    ServiceType = g.Key,
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .ToList();

            // Build CSV content
            var csvBuilder = new StringBuilder();

            // Report Title
            csvBuilder.AppendLine("LGU Comprehensive Report");
            csvBuilder.AppendLine($"Generated on: {DateTime.Now}");
            csvBuilder.AppendLine();

            // Application Status Section
            csvBuilder.AppendLine("Section: Application Status Overview");
            csvBuilder.AppendLine("Status,Count");
            foreach (var status in applicationStatuses)
            {
                csvBuilder.AppendLine($"{status.Status},{status.Count}");
            }
            csvBuilder.AppendLine();

            // Service Requests Section
            csvBuilder.AppendLine("Section: Service Requests Overview");
            csvBuilder.AppendLine("Service Name,Number of Requests");
            foreach (var service in servicesReport)
            {
                csvBuilder.AppendLine($"{service.ServiceName},{service.Count}");
            }
            csvBuilder.AppendLine();

            // Payments Section
            csvBuilder.AppendLine("Section: Payments Overview");
            csvBuilder.AppendLine("Service Type,Total Amount Collected");
            foreach (var payment in paymentsReport)
            {
                csvBuilder.AppendLine($"{payment.ServiceType},{payment.TotalAmount:C}");
            }
            csvBuilder.AppendLine();

            // Summary Section
            csvBuilder.AppendLine("Summary:");
            csvBuilder.AppendLine($"Total Applications Processed: {applicationStatuses.Sum(a => a.Count)}");
            csvBuilder.AppendLine($"Total Service Requests: {servicesReport.Sum(s => s.Count)}");
            csvBuilder.AppendLine($"Total Payments Collected: {paymentsReport.Sum(p => p.TotalAmount):C}");
            csvBuilder.AppendLine();

            // Return the CSV file
            var bytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());
            var result = new FileContentResult(bytes, "text/csv")
            {
                FileDownloadName = $"LGU_Report_{DateTime.Now:yyyyMMddHHmmss}.csv"
            };

            return result;
        }

        [HttpPost]
        public async Task<IActionResult> DownloadPDFReport([FromBody] ChartImagesRequest charts)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.A4, 25, 25, 30, 30);
                PdfWriter writer = PdfWriter.GetInstance(pdfDoc, stream);
                pdfDoc.Open();

                // Add the system name and logo
                PdfPTable headerTable = new PdfPTable(2);
                headerTable.WidthPercentage = 100;
                headerTable.SetWidths(new float[] { 1, 4 });

                // Add logo
                string logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo.png");
                if (System.IO.File.Exists(logoPath))
                {
                    Image logo = Image.GetInstance(logoPath);
                    logo.ScaleToFit(50f, 50f);
                    PdfPCell logoCell = new PdfPCell(logo) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_LEFT };
                    headerTable.AddCell(logoCell);
                }
                else
                {
                    // Debugging information
                    Console.WriteLine($"Logo not found at {logoPath}");
                    PdfPCell placeholderCell = new PdfPCell(new Phrase("Logo Missing")) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_LEFT };
                    headerTable.AddCell(placeholderCell);
                }

                // Add system name
                PdfPCell titleCell = new PdfPCell(new Phrase("AreEyeP Reports", new Font(Font.FontFamily.HELVETICA, 16, Font.BOLD)))
                {
                    Border = Rectangle.NO_BORDER,
                    HorizontalAlignment = Element.ALIGN_LEFT,
                    VerticalAlignment = Element.ALIGN_MIDDLE
                };
                headerTable.AddCell(titleCell);

                pdfDoc.Add(headerTable);
                pdfDoc.Add(new Paragraph(" ")); // Empty line

                // Add generation date
                pdfDoc.Add(new Paragraph($"Generated on: {DateTime.Now.ToString("MMMM dd, yyyy")}", new Font(Font.FontFamily.HELVETICA, 10)));
                pdfDoc.Add(new Paragraph(" ")); // Empty line

                // Application Status Section
                pdfDoc.Add(new Paragraph("Application Status Overview:", new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD)));
                PdfPTable appTable = new PdfPTable(2);
                appTable.AddCell("Status");
                appTable.AddCell("Count");

                var applicationStatuses = _context.BurialApplications
                    .GroupBy(a => a.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToList();

                foreach (var status in applicationStatuses)
                {
                    appTable.AddCell(status.Status);
                    appTable.AddCell(status.Count.ToString());
                }
                pdfDoc.Add(appTable);

                // Add Application Status Chart
                if (!string.IsNullOrEmpty(charts.ApplicationChart))
                {
                    pdfDoc.Add(new Paragraph("\n"));
                    var appChartBytes = Convert.FromBase64String(charts.ApplicationChart.Split(',')[1]);
                    Image appChartImage = Image.GetInstance(appChartBytes);
                    appChartImage.ScaleToFit(500f, 300f);
                    pdfDoc.Add(appChartImage);
                }

                // Repeat for other sections...

                // Close the PDF document
                pdfDoc.Close();
                return File(stream.ToArray(), "application/pdf", "LGU_Report.pdf");
            }
        }

        public class ChartImagesRequest
        {
            public string ApplicationChart { get; set; }
            public string ServiceChart { get; set; }
            public string PaymentChart { get; set; }
        }

    }
}
