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
                writer.PageEvent = new PdfPageNumberHelper(); // Correct the assignment
                pdfDoc.Open();

                // Add Cover Page
                AddCoverPage(pdfDoc);

                // Application Status Section
                AddSectionHeader(pdfDoc, "Application Status Overview");
                PdfPTable appTable = CreateTable(new[] { "Status", "Count" });

                var applicationStatuses = _context.BurialApplications
                    .GroupBy(a => a.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToList();

                foreach (var status in applicationStatuses)
                {
                    AddTableRow(appTable, status.Status, status.Count.ToString());
                }
                pdfDoc.Add(appTable);

                // Add Application Status Chart
                if (!string.IsNullOrEmpty(charts.ApplicationChart))
                {
                    AddChartToPDF(pdfDoc, charts.ApplicationChart);
                }

                // Service Requests Section
                AddSectionHeader(pdfDoc, "Service Requests Overview");
                PdfPTable serviceTable = CreateTable(new[] { "Service Type", "Count" });

                var allServices = _context.Services.Select(s => s.ServiceName).ToList();
                var serviceRequestCounts = _context.ServiceRequests
                    .GroupBy(sr => sr.ServiceType)
                    .Select(g => new { ServiceType = g.Key, Count = g.Count() })
                    .ToList();

                foreach (var service in allServices)
                {
                    var count = serviceRequestCounts.FirstOrDefault(s => s.ServiceType == service)?.Count ?? 0;
                    AddTableRow(serviceTable, service, count.ToString());
                }
                pdfDoc.Add(serviceTable);

                // Add Service Requests Chart
                if (!string.IsNullOrEmpty(charts.ServiceChart))
                {
                    AddChartToPDF(pdfDoc, charts.ServiceChart);
                }

                // Payments Section
                AddSectionHeader(pdfDoc, "Payments Overview");
                PdfPTable paymentTable = CreateTable(new[] { "Service Type", "Total Amount" });

                var paymentsReport = _context.ClientPayments
                    .GroupBy(p => p.ServiceType)
                    .Select(g => new { ServiceType = g.Key, TotalAmount = g.Sum(p => p.Amount) })
                    .ToList();

                foreach (var payment in paymentsReport)
                {
                    AddTableRow(paymentTable, payment.ServiceType, payment.TotalAmount.ToString("C"));
                }
                pdfDoc.Add(paymentTable);

                // Add Payments Chart
                if (!string.IsNullOrEmpty(charts.PaymentChart))
                {
                    AddChartToPDF(pdfDoc, charts.PaymentChart);
                }

                pdfDoc.Close();
                return File(stream.ToArray(), "application/pdf", "LGU_Report.pdf");
            }
        }

        // Helper Methods

        private void AddCoverPage(Document pdfDoc)
        {
            PdfPTable headerTable = new PdfPTable(1) { WidthPercentage = 100 };
            headerTable.DefaultCell.Border = Rectangle.NO_BORDER;
            headerTable.DefaultCell.HorizontalAlignment = Element.ALIGN_CENTER;

            string logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo.png");
            if (System.IO.File.Exists(logoPath))
            {
                Image logo = Image.GetInstance(logoPath);
                logo.ScaleToFit(100f, 100f);
                PdfPCell logoCell = new PdfPCell(logo)
                {
                    Border = Rectangle.NO_BORDER,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    PaddingBottom = 20
                };
                headerTable.AddCell(logoCell);
            }

            headerTable.AddCell(new Phrase("AreEyeP Reports", new Font(Font.FontFamily.HELVETICA, 24, Font.BOLD)));
            headerTable.AddCell(new Phrase($"Generated on: {DateTime.Now:MMMM dd, yyyy}", new Font(Font.FontFamily.HELVETICA, 12)));
            pdfDoc.Add(headerTable);
            pdfDoc.NewPage();
        }

        private void AddSectionHeader(Document pdfDoc, string title)
        {
            Paragraph header = new Paragraph(title, new Font(Font.FontFamily.HELVETICA, 16, Font.BOLD))
            {
                SpacingBefore = 20f,
                SpacingAfter = 10f
            };
            pdfDoc.Add(header);
        }

        private PdfPTable CreateTable(string[] headers)
        {
            PdfPTable table = new PdfPTable(headers.Length) { WidthPercentage = 100, SpacingBefore = 10f, SpacingAfter = 10f };
            foreach (string header in headers)
            {
                PdfPCell cell = new PdfPCell(new Phrase(header, new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD)))
                {
                    BackgroundColor = BaseColor.LIGHT_GRAY,
                    HorizontalAlignment = Element.ALIGN_CENTER
                };
                table.AddCell(cell);
            }
            return table;
        }

        private void AddTableRow(PdfPTable table, string col1, string col2)
        {
            table.AddCell(new PdfPCell(new Phrase(col1, new Font(Font.FontFamily.HELVETICA, 10))));
            table.AddCell(new PdfPCell(new Phrase(col2, new Font(Font.FontFamily.HELVETICA, 10))) { HorizontalAlignment = Element.ALIGN_CENTER });
        }

        private void AddChartToPDF(Document pdfDoc, string chartBase64)
        {
            pdfDoc.Add(new Paragraph("\n"));
            var chartBytes = Convert.FromBase64String(chartBase64.Split(',')[1]);
            Image chartImage = Image.GetInstance(chartBytes);
            chartImage.ScaleToFit(500f, 300f);
            chartImage.Alignment = Element.ALIGN_CENTER;
            pdfDoc.Add(chartImage);
        }

        // Page Number Helper Class
        public class PdfPageNumberHelper : PdfPageEventHelper
        {
            public override void OnEndPage(PdfWriter writer, Document document)
            {
                PdfContentByte cb = writer.DirectContent;
                ColumnText.ShowTextAligned(cb, Element.ALIGN_CENTER,
                    new Phrase($"Page {writer.PageNumber}", new Font(Font.FontFamily.HELVETICA, 10)),
                    (document.Left + document.Right) / 2, document.Bottom - 20, 0);
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
