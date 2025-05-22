// NoorRAC/Services/IPdfGenerationService.cs
using NoorRAC.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NoorRAC.Services
{
    public interface IPdfGenerationService
    {
        Task GenerateFinancialReportAsync(
            string filePath,
            DateTime fromDate,
            DateTime toDate,
            FinancialOverviewStats overviewStats,
            List<FinancialTransactionRecord> transactions,
            List<DailyFinancialSummary> dailySummaries // For chart data in PDF if desired
            );
    }
}