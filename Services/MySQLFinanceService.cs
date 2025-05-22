using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient; // Or using MySqlConnector;
using NoorRAC.Models;
using NoorRAC.ViewModels; // Or NoorRAC.Models
using System;
using System.Data;
using System.Globalization;
using System.Text;
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

        public async Task<List<DailyFinancialSummary>> GetDailySummariesAsync(DateTime fromDate, DateTime toDate)
        {
            var summaries = new List<DailyFinancialSummary>();
            // Ensure toDate includes the whole day
            var inclusiveToDate = toDate.Date.AddDays(1).AddTicks(-1);

            var query = @"
                SELECT 
                    summary_date,
                    COALESCE(SUM(payment_amount), 0) AS TotalPayments,
                    COALESCE(SUM(expense_amount), 0) AS TotalExpenses
                FROM (
                    SELECT 
                        DATE(payment_date) AS summary_date,
                        amount AS payment_amount,
                        0 AS expense_amount
                    FROM payment
                    WHERE payment_date >= @FromDate AND payment_date <= @ToDate
                    UNION ALL
                    SELECT 
                        DATE(date) AS summary_date,
                        0 AS payment_amount,
                        amount AS expense_amount
                    FROM expense
                    WHERE date >= @FromDate AND date <= @ToDate
                ) AS combined_data
                GROUP BY summary_date
                ORDER BY summary_date ASC;";

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FromDate", fromDate.Date);
                        command.Parameters.AddWithValue("@ToDate", inclusiveToDate);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                summaries.Add(new DailyFinancialSummary
                                {
                                    Date = reader.GetDateTime("summary_date"),
                                    TotalPayments = reader.GetDecimal("TotalPayments"),
                                    TotalExpenses = reader.GetDecimal("TotalExpenses")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching daily financial summaries: {ex.Message}");
                // Handle or rethrow
            }
            return summaries;
        }

        public async Task<FinancialOverviewStats> GetFinancialOverviewStatsAsync(DateTime fromDate, DateTime toDate)
        {
            var stats = new FinancialOverviewStats();
            var inclusiveToDate = toDate.Date.AddDays(1).AddTicks(-1);

            string queryPayments = "SELECT COALESCE(SUM(amount), 0) FROM payment WHERE payment_date >= @FromDate AND payment_date <= @ToDate;";
            string queryExpenses = "SELECT COALESCE(SUM(amount), 0) FROM expense WHERE date >= @FromDate AND date <= @ToDate;";

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var cmdPayments = new MySqlCommand(queryPayments, connection))
                    {
                        cmdPayments.Parameters.AddWithValue("@FromDate", fromDate.Date);
                        cmdPayments.Parameters.AddWithValue("@ToDate", inclusiveToDate);
                        stats.TotalIncomeForPeriod = Convert.ToDecimal(await cmdPayments.ExecuteScalarAsync());
                    }

                    using (var cmdExpenses = new MySqlCommand(queryExpenses, connection))
                    {
                        cmdExpenses.Parameters.AddWithValue("@FromDate", fromDate.Date);
                        cmdExpenses.Parameters.AddWithValue("@ToDate", inclusiveToDate);
                        stats.TotalExpensesForPeriod = Convert.ToDecimal(await cmdExpenses.ExecuteScalarAsync());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching financial overview stats: {ex.Message}");
            }
            return stats;
        }

        private void BuildCombinedWhereClause(StringBuilder query, List<MySqlParameter> parameters, DateTime fromDate, DateTime toDate, string? searchTerm)
        {
            var conditions = new List<string>();
            // Date conditions are applied in the subqueries of the main UNION ALL query

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                conditions.Add("(Name LIKE @SearchTerm OR AmountStr LIKE @SearchTerm OR Details LIKE @SearchTerm)");
                parameters.Add(new MySqlParameter("@SearchTerm", $"%{searchTerm}%"));
            }

            if (conditions.Count > 0)
            {
                query.Append(" WHERE ");
                query.Append(string.Join(" AND ", conditions));
            }
        }


        public async Task<List<FinancialTransactionRecord>> GetCombinedTransactionsAsync(
            DateTime fromDate, DateTime toDate, int pageNumber, int pageSize, string? searchTerm)
        {
            var transactions = new List<FinancialTransactionRecord>();
            var inclusiveToDate = toDate.Date.AddDays(1).AddTicks(-1);

            var baseQuery = new StringBuilder($@"
                SELECT OriginalId, Name, Amount, Type, Date, Details, SourceTable, CAST(Amount AS CHAR) as AmountStr
                FROM (
                    SELECT 
                        p.id AS OriginalId,
                        c.name AS Name,
                        p.amount AS Amount,
                        'Income' AS Type,
                        p.payment_date AS Date,
                        p.payment_method AS Details,
                        'Payment' AS SourceTable
                    FROM payment p
                    JOIN customer c ON p.customer_id = c.id
                    WHERE p.payment_date >= @FromDate AND p.payment_date <= @ToDate
                    
                    UNION ALL
                    
                    SELECT 
                        e.id AS OriginalId,
                        e.description AS Name,
                        e.amount AS Amount,
                        'Expense' AS Type,
                        e.date AS Date,
                        cr.registration_number AS Details,  -- Assuming car reg num for expense details
                        'Expense' AS SourceTable
                    FROM expense e
                    LEFT JOIN car cr ON e.car_id = cr.id   -- Assuming car table has 'id'
                    WHERE e.date >= @FromDate AND e.date <= @ToDate
                ) AS CombinedTransactions
            ");

            var sqlParameters = new List<MySqlParameter>
            {
                new MySqlParameter("@FromDate", fromDate.Date),
                new MySqlParameter("@ToDate", inclusiveToDate)
            };

            BuildCombinedWhereClause(baseQuery, sqlParameters, fromDate, toDate, searchTerm);

            baseQuery.Append(" ORDER BY Date DESC, OriginalId DESC");
            baseQuery.Append(" LIMIT @PageSize OFFSET @Offset;");
            sqlParameters.Add(new MySqlParameter("@PageSize", pageSize));
            sqlParameters.Add(new MySqlParameter("@Offset", (pageNumber - 1) * pageSize));

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new MySqlCommand(baseQuery.ToString(), connection))
                    {
                        command.Parameters.AddRange(sqlParameters.ToArray());
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                transactions.Add(new FinancialTransactionRecord
                                {
                                    OriginalId = reader.GetInt32("OriginalId"),
                                    Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? "N/A" : reader.GetString("Name"),
                                    Amount = reader.GetDecimal("Amount"), // Assuming amount is stored as DECIMAL or compatible
                                    Type = (TransactionType)Enum.Parse(typeof(TransactionType), reader.GetString("Type")),
                                    Date = reader.GetDateTime("Date"),
                                    Details = reader.IsDBNull(reader.GetOrdinal("Details")) ? "N/A" : reader.GetString("Details"),
                                    SourceTable = reader.GetString("SourceTable")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching combined transactions: {ex.Message}");
            }
            return transactions;
        }

        public async Task<int> GetTotalCombinedTransactionsCountAsync(DateTime fromDate, DateTime toDate, string? searchTerm)
        {
            var inclusiveToDate = toDate.Date.AddDays(1).AddTicks(-1);
            var countQuery = new StringBuilder($@"
                 SELECT COUNT(*)
                 FROM (
                    SELECT p.id AS OriginalId, c.name AS Name, p.amount AS Amount, 'Income' AS Type, p.payment_date AS Date, p.payment_method AS Details, 'Payment' AS SourceTable, CAST(p.amount AS CHAR) as AmountStr FROM payment p JOIN customer c ON p.customer_id = c.id WHERE p.payment_date >= @FromDate AND p.payment_date <= @ToDate
                    UNION ALL
                    SELECT e.id AS OriginalId, e.description AS Name, e.amount AS Amount, 'Expense' AS Type, e.date AS Date, cr.registration_number AS Details, 'Expense' AS SourceTable, CAST(e.amount AS CHAR) as AmountStr FROM expense e LEFT JOIN car cr ON e.car_id = cr.id WHERE e.date >= @FromDate AND e.date <= @ToDate
                 ) AS CombinedTransactions
            ");

            var sqlParameters = new List<MySqlParameter>
            {
                new MySqlParameter("@FromDate", fromDate.Date),
                new MySqlParameter("@ToDate", inclusiveToDate)
            };

            BuildCombinedWhereClause(countQuery, sqlParameters, fromDate, toDate, searchTerm);

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new MySqlCommand(countQuery.ToString(), connection))
                    {
                        command.Parameters.AddRange(sqlParameters.ToArray());
                        var result = await command.ExecuteScalarAsync();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching total combined transactions count: {ex.Message}");
                return 0;
            }
        }

        public async Task<List<FinancialTransactionRecord>> GetAllTransactionsForReportAsync(DateTime fromDate, DateTime toDate)
        {
            // This method is similar to GetCombinedTransactionsAsync but without pagination
            // It's used specifically for fetching all data for a PDF report within the date range.
            var transactions = new List<FinancialTransactionRecord>();
            var inclusiveToDate = toDate.Date.AddDays(1).AddTicks(-1);

            var query = $@"
                SELECT OriginalId, Name, Amount, Type, Date, Details, SourceTable
                FROM (
                    SELECT 
                        p.id AS OriginalId,
                        c.name AS Name,
                        p.amount AS Amount,
                        'Income' AS Type,
                        p.payment_date AS Date,
                        p.payment_method AS Details,
                        'Payment' AS SourceTable
                    FROM payment p
                    JOIN customer c ON p.customer_id = c.id
                    WHERE p.payment_date >= @FromDate AND p.payment_date <= @ToDate
                    
                    UNION ALL
                    
                    SELECT 
                        e.id AS OriginalId,
                        e.description AS Name,
                        e.amount AS Amount,
                        'Expense' AS Type,
                        e.date AS Date,
                        cr.registration_number AS Details,
                        'Expense' AS SourceTable
                    FROM expense e
                    LEFT JOIN car cr ON e.car_id = cr.id
                    WHERE e.date >= @FromDate AND e.date <= @ToDate
                ) AS CombinedTransactions
                ORDER BY Date DESC, OriginalId DESC;";

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FromDate", fromDate.Date);
                        command.Parameters.AddWithValue("@ToDate", inclusiveToDate);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                transactions.Add(new FinancialTransactionRecord
                                {
                                    OriginalId = reader.GetInt32("OriginalId"),
                                    Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? "N/A" : reader.GetString("Name"),
                                    Amount = reader.GetDecimal("Amount"),
                                    Type = (TransactionType)Enum.Parse(typeof(TransactionType), reader.GetString("Type")),
                                    Date = reader.GetDateTime("Date"),
                                    Details = reader.IsDBNull(reader.GetOrdinal("Details")) ? "N/A" : reader.GetString("Details"),
                                    SourceTable = reader.GetString("SourceTable")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching all transactions for report: {ex.Message}");
            }
            return transactions;
        }
    }
}