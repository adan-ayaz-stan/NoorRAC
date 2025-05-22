// NoorRAC/Services/IDashboardService.cs
using NoorRAC.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NoorRAC.Services
{
    public interface IDashboardService
    {
        Task<DashboardStats> GetFinancialSummaryAsync(); // Compares current month to previous month
        Task<List<FleetSummaryCar>> GetFleetSummaryAsync(int maxCars = 8);
        Task<List<RentalDisplayRecord>> GetRecentRentalsAsync(string timePeriod); // e.g., "This Week", "This Month", "Last 7 Days"

        Task<List<MonthlyFinancialChartData>> GetFinancialChartDataAsync(int months = 6); // Get data for the last N months
        Task<List<MonthlyRentalCountChartData>> GetRentalChartDataAsync(int months = 6);  // Get data for the last N months
    }
    public class MonthlyFinancialChartData
    {
        public string MonthYearLabel { get; set; } = string.Empty; // e.g., "Jan 23"
        public double Income { get; set; }
        public double Expenses { get; set; }
    }

    public class MonthlyRentalCountChartData
    {
        public string MonthYearLabel { get; set; } = string.Empty; // e.g., "Jan 23"
        public int RentalCount { get; set; }
    }
}