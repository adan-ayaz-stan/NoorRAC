// NoorRAC/Services/QuestPdfGenerationService.cs
// Install QuestPDF NuGet package: Install-Package QuestPDF
using NoorRAC.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NoorRAC.Services
{
    public class QuestPdfGenerationService : IPdfGenerationService
    {
        public Task GenerateFinancialReportAsync(
            string filePath,
            DateTime fromDate,
            DateTime toDate,
            FinancialOverviewStats overviewStats,
            List<FinancialTransactionRecord> transactions,
            List<DailyFinancialSummary> dailySummaries)
        {
            return Task.Run(() => // Run on a background thread
            {
                QuestPDF.Settings.License = LicenseType.Community; // Or LicenseType.Commercial if applicable
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(30, Unit.Point);
                        page.DefaultTextStyle(ts => ts.FontSize(10));

                        page.Header()
                            .AlignCenter()
                            .Text($"Financial Report: {fromDate:dd-MM-yyyy} to {toDate:dd-MM-yyyy}")
                            .SemiBold().FontSize(16);

                        page.Content()
                            .PaddingVertical(1, Unit.Centimetre)
                            .Column(column =>
                            {
                                column.Spacing(20);

                                // Overview Stats
                                column.Item().Text("Financial Overview").Bold().FontSize(14);
                                column.Item().Grid(grid =>
                                {
                                    grid.Columns(2);
                                    grid.Item().Text($"Total Income: {overviewStats.TotalIncomeForPeriod:C}");
                                    grid.Item().Text($"Total Expenses: {overviewStats.TotalExpensesForPeriod:C}");
                                    grid.Item().Text($"Net Profit/Loss: {overviewStats.NetProfitForPeriod:C}");
                                });

                                column.Item().PaddingTop(10); // Spacing

                                // Transactions Table
                                column.Item().Text("Transaction Details").Bold().FontSize(14);
                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(1.5f); // Date
                                        columns.RelativeColumn(3);   // Name/Description
                                        columns.RelativeColumn(1);   // Type
                                        columns.RelativeColumn(1.5f); // Amount
                                        columns.RelativeColumn(3);   // Details
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Text("Date").Bold();
                                        header.Cell().Text("Name/Description").Bold();
                                        header.Cell().Text("Type").Bold();
                                        header.Cell().Text("Amount").Bold().AlignCenter();
                                        header.Cell().Text("Details").Bold();
                                    });

                                    foreach (var tx in transactions)
                                    {
                                        table.Cell().Text(tx.Date.ToString("dd-MM-yyyy"));
                                        table.Cell().Text(tx.Name);
                                        table.Cell().Text(tx.Type.ToString());
                                        table.Cell().Text(tx.Amount.ToString("N2")).AlignCenter(); // Format as number
                                        table.Cell().Text(tx.Details);
                                    }
                                });

                                // Optionally, add chart image or data summary here if you convert chart to image
                                // For simplicity, I'm omitting direct chart rendering in PDF here.
                                // You could list the daily summaries:
                                if (dailySummaries.Any())
                                {
                                    column.Item().PaddingTop(10).Text("Daily Summaries (Payments vs Expenses)").Bold().FontSize(12);
                                    foreach (var daily in dailySummaries)
                                    {
                                        column.Item().Text($"{daily.Date:dd-MM-yy}: Payments {daily.TotalPayments:N0}, Expenses {daily.TotalExpenses:N0}");
                                    }
                                }


                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("Page ");
                                x.CurrentPageNumber();
                                x.Span(" of ");
                                x.TotalPages();
                            });
                    });
                })
                .GeneratePdf(filePath);
            });
        }
    }
}