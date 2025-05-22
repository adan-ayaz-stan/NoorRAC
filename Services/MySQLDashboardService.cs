// NoorRAC/Services/MySqlDashboardService.cs
using MySql.Data.MySqlClient;
using NoorRAC.Models;
using NoorRAC.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoorRAC.Services
{
    public class MySqlDashboardService : IDashboardService
    {
        private readonly string _connectionString = "server=localhost;database=NoorRAC;uid=root;pwd=root;";

        private async Task<decimal> GetTotalPaymentsForPeriodAsync(MySqlConnection connection, DateTime startDate, DateTime endDate)
        {
            decimal total = 0;
            var query = "SELECT SUM(amount) FROM payment WHERE payment_date >= @StartDate AND payment_date <= @EndDate;";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@StartDate", startDate);
                command.Parameters.AddWithValue("@EndDate", endDate);
                var result = await command.ExecuteScalarAsync();
                if (result != DBNull.Value && result != null)
                {
                    total = Convert.ToDecimal(result);
                }
            }
            return total;
        }

        private async Task<decimal> GetTotalExpensesForPeriodAsync(MySqlConnection connection, DateTime startDate, DateTime endDate)
        {
            decimal total = 0;
            var query = "SELECT SUM(amount) FROM expense WHERE date >= @StartDate AND date <= @EndDate;";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@StartDate", startDate);
                command.Parameters.AddWithValue("@EndDate", endDate);
                var result = await command.ExecuteScalarAsync();
                if (result != DBNull.Value && result != null)
                {
                    total = Convert.ToDecimal(result);
                }
            }
            return total;
        }

        private FinancialMetric CalculateMetric(decimal current, decimal previous, string periodDescription = "vs last month")
        {
            var metric = new FinancialMetric
            {
                CurrentPeriodAmount = $"Rs. {current:N0}", // Format as currency
                ChangeDescription = periodDescription
            };

            if (previous == 0)
            {
                metric.PercentageChange = current > 0 ? "+100%" : "0%"; // Or "N/A" or "∞"
                metric.IsIncrease = current > 0;
            }
            else
            {
                decimal change = ((current - previous) / previous) * 100;
                metric.PercentageChange = $"{(change >= 0 ? "+" : "")}{change:F1}%";
                metric.IsIncrease = change >= 0;
            }
            return metric;
        }

        public async Task<DashboardStats> GetFinancialSummaryAsync()
        {
            var stats = new DashboardStats();
            DateTime today = DateTime.Today;

            // Current Month
            DateTime currentMonthStart = new DateTime(today.Year, today.Month, 1);
            DateTime currentMonthEnd = currentMonthStart.AddMonths(1).AddDays(-1);

            // Previous Month
            DateTime previousMonthStart = currentMonthStart.AddMonths(-1);
            DateTime previousMonthEnd = currentMonthStart.AddDays(-1);

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Current Period Data
                    decimal currentTurnover = await GetTotalPaymentsForPeriodAsync(connection, currentMonthStart, currentMonthEnd);
                    decimal currentExpenses = await GetTotalExpensesForPeriodAsync(connection, currentMonthStart, currentMonthEnd);
                    decimal currentIncome = currentTurnover - currentExpenses;

                    // Previous Period Data
                    decimal previousTurnover = await GetTotalPaymentsForPeriodAsync(connection, previousMonthStart, previousMonthEnd);
                    decimal previousExpenses = await GetTotalExpensesForPeriodAsync(connection, previousMonthStart, previousMonthEnd);
                    decimal previousIncome = previousTurnover - previousExpenses;

                    // Calculate Metrics
                    stats.Turnover = CalculateMetric(currentTurnover, previousTurnover);
                    stats.Outflow = CalculateMetric(currentExpenses, previousExpenses);
                    stats.Income = CalculateMetric(currentIncome, previousIncome);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting financial summary: {ex.Message}");
                // Return stats with default 0 values if an error occurs
            }
            return stats;
        }

        public async Task<List<FleetSummaryCar>> GetFleetSummaryAsync(int maxCars = 8)
        {
            var fleetSummary = new List<FleetSummaryCar>();
            DateTime today = DateTime.Today;

            // This query aims to get cars, their current rental end date (if any),
            // and their next booking start date (if any).
            // It's a bit complex due to multiple joins and conditional logic.
            var query = @"
                SELECT 
                    c.id AS CarId, 
                    c.registration_number AS RegistrationNumber,
                    MAX(CASE WHEN r_current.start_date <= @Today AND r_current.end_date >= @Today THEN r_current.end_date ELSE NULL END) AS CurrentRentalEndDate,
                    MIN(CASE WHEN r_future.start_date > @Today THEN r_future.start_date ELSE NULL END) AS NextBookingStartDate
                FROM car c
                LEFT JOIN rental r_current ON c.registration_number = r_current.car_registration_no 
                                          AND r_current.start_date <= @Today AND r_current.end_date >= @Today
                LEFT JOIN rental r_future ON c.registration_number = r_future.car_registration_no 
                                          AND r_future.start_date > @Today 
                GROUP BY c.id, c.registration_number
                ORDER BY CurrentRentalEndDate ASC, NextBookingStartDate ASC -- Prioritize cars returning soon or booked soon
                LIMIT @MaxCars;
            ";
            // A more sophisticated ordering might be needed based on exact "arriving earliest" definition.
            // For example, if a car is available, it should probably appear after those arriving soon.
            // This example prioritizes cars that are currently rented and arriving soon.

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Today", today.Date);
                        command.Parameters.AddWithValue("@MaxCars", maxCars);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var car = new FleetSummaryCar
                                {
                                    CarId = reader.GetInt32("CarId"),
                                    RegistrationNumber = reader.GetString("RegistrationNumber")
                                };

                                if (!reader.IsDBNull(reader.GetOrdinal("CurrentRentalEndDate")))
                                {
                                    car.CurrentRentalEndDate = reader.GetDateTime("CurrentRentalEndDate");
                                    car.IsCurrentlyRented = true;
                                    TimeSpan timeTillArrival = car.CurrentRentalEndDate.Value.Date - today.Date;
                                    if (timeTillArrival.TotalDays < 0) car.ArrivingStatus = "Overdue"; // Should not happen with query logic
                                    else if (timeTillArrival.TotalDays == 0) car.ArrivingStatus = "Today";
                                    else if (timeTillArrival.TotalDays == 1) car.ArrivingStatus = "Tomorrow";
                                    else car.ArrivingStatus = $"In {timeTillArrival.TotalDays} Days";
                                }
                                else
                                {
                                    car.ArrivingStatus = "Available";
                                    car.IsCurrentlyRented = false;
                                }

                                if (!reader.IsDBNull(reader.GetOrdinal("NextBookingStartDate")))
                                {
                                    car.NextBookingStartDate = reader.GetDateTime("NextBookingStartDate");
                                    car.NextRentalStatus = $"{car.NextBookingStartDate.Value:dd-MM-yy}";
                                }
                                else
                                {
                                    car.NextRentalStatus = "No Upcoming Rental";
                                }

                                fleetSummary.Add(car);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting fleet summary: {ex.Message}");
            }
            return fleetSummary;
        }

        public async Task<List<RentalDisplayRecord>> GetRecentRentalsAsync(string timePeriod)
        {
            var rentals = new List<RentalDisplayRecord>();
            DateTime today = DateTime.Today;
            DateTime startDateFilter;

            switch (timePeriod)
            {
                case "This Week": // Assuming week starts on Monday
                    int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                    startDateFilter = today.AddDays(-1 * diff).Date;
                    break;
                case "This Month":
                    startDateFilter = new DateTime(today.Year, today.Month, 1);
                    break;
                case "Last 7 Days":
                    startDateFilter = today.AddDays(-6).Date; // Includes today
                    break;
                case "Last 30 Days":
                    startDateFilter = today.AddDays(-29).Date; // Includes today
                    break;
                default: // "All Time" or unrecognized
                    startDateFilter = DateTime.MinValue;
                    break;
            }

            // If "All Time", we might want to limit results or order differently.
            // For now, just fetching all after startDateFilter, then taking top N in ViewModel.
            var query = new StringBuilder(@"
                SELECT r.id AS ID, cust.name AS ClientName, 
                       CONCAT(c.car_make, ' ', c.car_model) AS CarType, 
                       r.car_registration_no AS CarNumber,
                       r.start_date as StartDate, r.end_date as EndDate
                FROM rental r
                JOIN customer cust ON r.customer_cnic = cust.cnic
                JOIN car c ON r.car_registration_no = c.registration_number
            ");

            var parameters = new Dictionary<string, object>();
            if (startDateFilter != DateTime.MinValue)
            {
                query.Append(" WHERE r.start_date >= @StartDateFilter "); // Or r.end_date for completed rentals in period
                parameters.Add("@StartDateFilter", startDateFilter);
            }
            query.Append(" ORDER BY r.start_date DESC, r.id DESC"); // Recent first
            // LIMIT can be added here or handled in VM. For dashboard, usually a small fixed number.

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new MySqlCommand(query.ToString(), connection))
                    {
                        foreach (var p in parameters) command.Parameters.AddWithValue(p.Key, p.Value);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                DateTime rentalStartDate = reader.GetDateTime("StartDate");
                                DateTime rentalEndDate = reader.GetDateTime("EndDate");
                                string status;
                                if (rentalStartDate <= today && rentalEndDate >= today)
                                    status = "Active";
                                else if (rentalEndDate < today)
                                    status = "Completed";
                                else // rentalStartDate > today
                                    status = "Upcoming";

                                rentals.Add(new RentalDisplayRecord
                                {
                                    Id = reader.GetInt32("ID"),
                                    ClientName = reader.GetString("ClientName"),
                                    CarType = reader.GetString("CarType"),
                                    CarRegistrationNumber = reader.GetString("CarNumber"),
                                    Status = status,
                                    StartDate = rentalStartDate,
                                    EndDate = rentalEndDate
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting recent rentals: {ex.Message}");
            }
            // The XAML takes top N, so no need to limit here strictly unless performance is an issue.
            return rentals;
        }

        public async Task<List<MonthlyFinancialChartData>> GetFinancialChartDataAsync(int months = 6)
        {
            var chartData = new List<MonthlyFinancialChartData>();
            DateTime today = DateTime.Today;

            for (int i = months - 1; i >= 0; i--) // Iterate from (months-1) down to 0 for chronological order
            {
                DateTime targetMonthDate = today.AddMonths(-i);
                DateTime monthStart = new DateTime(targetMonthDate.Year, targetMonthDate.Month, 1);
                DateTime monthEnd = monthStart.AddMonths(1).AddDays(-1);

                decimal incomeForMonth = 0; // This is typically Payments
                decimal expensesForMonth = 0;

                try
                {
                    using (var connection = new MySqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();
                        incomeForMonth = await GetTotalPaymentsForPeriodAsync(connection, monthStart, monthEnd); // Reuse existing helper
                        expensesForMonth = await GetTotalExpensesForPeriodAsync(connection, monthStart, monthEnd); // Reuse existing helper
                    }
                    chartData.Add(new MonthlyFinancialChartData
                    {
                        MonthYearLabel = monthStart.ToString("MMM yy"), // e.g., "Jan 23"
                        Income = (double)incomeForMonth,
                        Expenses = (double)expensesForMonth
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching financial chart data for {monthStart:MMM yy}: {ex.Message}");
                    // Add a point with 0 values for this month if error occurs to maintain structure
                    chartData.Add(new MonthlyFinancialChartData { MonthYearLabel = monthStart.ToString("MMM yy"), Income = 0, Expenses = 0 });
                }
            }
            return chartData;
        }

        public async Task<List<MonthlyRentalCountChartData>> GetRentalChartDataAsync(int months = 6)
        {
            var chartData = new List<MonthlyRentalCountChartData>();
            DateTime today = DateTime.Today;

            for (int i = months - 1; i >= 0; i--)
            {
                DateTime targetMonthDate = today.AddMonths(-i);
                DateTime monthStart = new DateTime(targetMonthDate.Year, targetMonthDate.Month, 1);
                DateTime monthEnd = monthStart.AddMonths(1).AddDays(-1);

                int rentalCount = 0;
                var query = "SELECT COUNT(id) FROM rental WHERE start_date <= @MonthEnd AND end_date >= @MonthStart;";
                // This counts rentals that were active at any point during the month.
                // If you want rentals *started* in the month: WHERE start_date >= @MonthStart AND start_date <= @MonthEnd

                try
                {
                    using (var connection = new MySqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();
                        using (var command = new MySqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@MonthStart", monthStart);
                            command.Parameters.AddWithValue("@MonthEnd", monthEnd);
                            var result = await command.ExecuteScalarAsync();
                            if (result != DBNull.Value && result != null)
                            {
                                rentalCount = Convert.ToInt32(result);
                            }
                        }
                    }
                    chartData.Add(new MonthlyRentalCountChartData
                    {
                        MonthYearLabel = monthStart.ToString("MMM yy"),
                        RentalCount = rentalCount
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching rental chart data for {monthStart:MMM yy}: {ex.Message}");
                    chartData.Add(new MonthlyRentalCountChartData { MonthYearLabel = monthStart.ToString("MMM yy"), RentalCount = 0 });
                }
            }
            return chartData;
        }
    }
}