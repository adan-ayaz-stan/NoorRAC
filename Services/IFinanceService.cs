using NoorRAC.Models;
using NoorRAC.ViewModels; // Or NoorRAC.Models
using System.Threading.Tasks;

namespace NoorRAC.Services
{
    public enum PdfReportTimespan
    {
        Today,
        ThisWeek,
        ThisMonth,
        LastSixMonths,
        Custom // If you allow custom range for PDF
    }
    public interface IFinanceService
    {
        /// <summary>
        /// Gets a financial summary (turnover, income, outflow) for a given period,
        /// potentially including comparison to the previous period.
        /// </summary>
        /// <param name="timePeriod">String representing the period (e.g., "This Week", "Last Month").</param>
        Task<FinanceSummary?> GetFinancialSummaryAsync(string timePeriod);

        // Add other Finance related methods here (GetPayments, GetExpenses etc.)
        Task<List<DailyFinancialSummary>> GetDailySummariesAsync(DateTime fromDate, DateTime toDate);
        Task<FinancialOverviewStats> GetFinancialOverviewStatsAsync(DateTime fromDate, DateTime toDate);
        Task<List<FinancialTransactionRecord>> GetCombinedTransactionsAsync(
            DateTime fromDate, DateTime toDate,
            int pageNumber, int pageSize,
            string? searchTerm);
        Task<int> GetTotalCombinedTransactionsCountAsync(
            DateTime fromDate, DateTime toDate,
            string? searchTerm);

        // For PDF Generation
        Task<List<FinancialTransactionRecord>> GetAllTransactionsForReportAsync(DateTime fromDate, DateTime toDate);
    }
}