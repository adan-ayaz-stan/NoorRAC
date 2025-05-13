using NoorRAC.ViewModels; // Or NoorRAC.Models
using System.Threading.Tasks;

namespace NoorRAC.Services
{
    public interface IFinanceService
    {
        /// <summary>
        /// Gets a financial summary (turnover, income, outflow) for a given period,
        /// potentially including comparison to the previous period.
        /// </summary>
        /// <param name="timePeriod">String representing the period (e.g., "This Week", "Last Month").</param>
        Task<FinanceSummary?> GetFinancialSummaryAsync(string timePeriod);

        // Add other Finance related methods here (GetPayments, GetExpenses etc.)
    }
}