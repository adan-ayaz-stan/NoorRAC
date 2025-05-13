using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient; // Or using MySqlConnector;
using NoorRAC.ViewModels; // Or NoorRAC.Models
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace NoorRAC.Services
{
    public class MySqlFinanceService : IFinanceService
    {
        private readonly string _connectionString;

        public MySqlFinanceService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                 ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<FinanceSummary?> GetFinancialSummaryAsync(string timePeriod)
        {
            var summary = new FinanceSummary();
            (DateTime currentStart, DateTime currentEnd) = CalculateDateRange(timePeriod);
            (DateTime prevStart, DateTime prevEnd) = CalculatePreviousDateRange(currentStart, currentEnd);

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // --- Get Current Period Data ---
                decimal currentIncome = await GetTotalPaymentsAsync(connection, currentStart, currentEnd);
                decimal currentOutflow = await GetTotalExpensesAsync(connection, currentStart, currentEnd);
                // Turnover could be defined differently, let's assume it's = Income for simplicity here
                decimal currentTurnover = currentIncome;

                // --- Get Previous Period Data ---
                decimal prevIncome = await GetTotalPaymentsAsync(connection, prevStart, prevEnd);
                decimal prevOutflow = await GetTotalExpensesAsync(connection, prevStart, prevEnd);
                decimal prevTurnover = prevIncome;

                // --- Format Results ---
                CultureInfo culture = new CultureInfo("en-PK"); // For Rupees formatting "Rs."

                summary.TurnoverAmountFormatted = currentTurnover.ToString("C0", culture); // C0 = Currency, 0 decimal places
                summary.TurnoverChangeFormatted = FormatChange(currentTurnover - prevTurnover, culture);
                summary.TurnoverChangeDescription = $"from previous {GetPeriodDescription(timePeriod)}";

                summary.IncomeAmountFormatted = currentIncome.ToString("C0", culture);
                summary.IncomeChangeFormatted = FormatChange(currentIncome - prevIncome, culture);
                summary.IncomeChangeDescription = $"from previous {GetPeriodDescription(timePeriod)}";

                summary.OutflowAmountFormatted = currentOutflow.ToString("C0", culture);
                summary.OutflowChangeFormatted = FormatChange(currentOutflow - prevOutflow, culture);
                summary.OutflowChangeDescription = $"from previous {GetPeriodDescription(timePeriod)}";

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetFinancialSummaryAsync: {ex.Message}");
                return null; // Indicate error
            }

            return summary;
        }


        // --- Helper Async Methods for DB Queries ---

        private async Task<decimal> GetTotalPaymentsAsync(MySqlConnection connection, DateTime startDate, DateTime endDate)
        {
            decimal total = 0;
            var query = "SELECT SUM(amount) FROM payment WHERE payment_date BETWEEN @StartDate AND @EndDate;";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);
            var result = await command.ExecuteScalarAsync();
            if (result != null && result != DBNull.Value)
            {
                total = Convert.ToDecimal(result);
            }
            return total;
        }

        private async Task<decimal> GetTotalExpensesAsync(MySqlConnection connection, DateTime startDate, DateTime endDate)
        {
            decimal total = 0;
            // Assuming only 'paid' expenses contribute to outflow
            var query = "SELECT SUM(amount) FROM expense WHERE payment_status = 'paid' AND date BETWEEN @StartDate AND @EndDate;";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);
            var result = await command.ExecuteScalarAsync();
            if (result != null && result != DBNull.Value)
            {
                total = Convert.ToDecimal(result);
            }
            return total;
        }


        // --- Helper Methods for Date Calculation & Formatting ---

        private (DateTime StartDate, DateTime EndDate) CalculateDateRange(string timePeriod)
        {
            // (Same logic as in MySqlRentalService - consider moving to a shared helper class)
            DateTime today = DateTime.Today;
            DateTime startDate = today;
            DateTime endDate = today;

            switch (timePeriod)
            {
                case "Last Week":
                    startDate = today.AddDays(-(int)today.DayOfWeek - 6); // Previous Sunday
                    endDate = startDate.AddDays(6); // Previous Saturday
                    break;
                case "This Month":
                    startDate = new DateTime(today.Year, today.Month, 1);
                    endDate = startDate.AddMonths(1).AddDays(-1);
                    break;
                case "Last Month":
                    startDate = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                    endDate = startDate.AddMonths(1).AddDays(-1);
                    break;
                case "This Week":
                default:
                    startDate = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Sunday); // Current week Sunday
                    endDate = startDate.AddDays(6); // Current week Saturday
                    break;
            }
            return (startDate, endDate);
        }

        private (DateTime StartDate, DateTime EndDate) CalculatePreviousDateRange(DateTime currentStartDate, DateTime currentEndDate)
        {
            TimeSpan duration = currentEndDate - currentStartDate;
            DateTime prevEndDate = currentStartDate.AddDays(-1);
            DateTime prevStartDate = prevEndDate - duration;
            return (prevStartDate, prevEndDate);
        }

        private string GetPeriodDescription(string timePeriod) => timePeriod.Replace("This ", "").ToLower();

        private string FormatChange(decimal change, CultureInfo culture)
        {
            string sign = change >= 0 ? "+" : "";
            return $"{sign}{change.ToString("N0", culture)}"; // N0 = Number, 0 decimal places
        }
    }
}