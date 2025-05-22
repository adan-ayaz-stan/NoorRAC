// NoorRAC/Services/MySQLPaymentService.cs
using Google.Protobuf.Compiler;
using Google.Protobuf.WellKnownTypes;
using MySql.Data.MySqlClient;
using NoorRAC.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text; // For StringBuilder
using System.Threading.Tasks;

namespace NoorRAC.Services
{
    public class MySQLPaymentService : IPaymentService
    {
        private readonly string _connectionString = "server=localhost;database=NoorRAC;uid=root;pwd=root;";

        public async Task<bool> AddPaymentAsync(PaymentRecord payment)
        {
            if (payment == null) throw new ArgumentNullException(nameof(payment));
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = @"INSERT INTO payment (customer_id, rental_id, amount, payment_method, payment_date, comment) 
                                  VALUES (@CustomerId, @RentalId, @Amount, @PaymentMethod, @PaymentDate, @Comment);
                                  SELECT LAST_INSERT_ID();"; // Get the ID of the inserted payment

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CustomerId", payment.CustomerId);
                        command.Parameters.AddWithValue("@RentalId", payment.RentalId.HasValue ? (object)payment.RentalId.Value : DBNull.Value);
                        command.Parameters.AddWithValue("@Amount", payment.AmountPaid); // Assuming DB 'amount' is INT
                        command.Parameters.AddWithValue("@PaymentMethod", payment.PaymentMethod);
                        command.Parameters.AddWithValue("@PaymentDate", payment.PaymentDate);
                        command.Parameters.AddWithValue("@Comment", payment.Comment);

                        var insertedId = await command.ExecuteScalarAsync();
                        if (insertedId != null && Convert.ToInt32(insertedId) > 0)
                        {
                            return true;
                        }
                        return false;
                    }
                }
            }
            catch (MySqlException ex) { Console.WriteLine($"MySQL Error adding payment: {ex.Message}"); return false; }
            catch (Exception ex) { Console.WriteLine($"General Error adding payment: {ex.Message}"); return false; }
        }

        public async Task<bool> UpdatePaymentAsync(PaymentRecord payment)
        {
            if (payment == null || payment.Id == 0) throw new ArgumentException("Payment or Payment ID is invalid for update.");
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = @"UPDATE payment SET 
                                    customer_id = @CustomerId, 
                                    rental_id = @RentalId, 
                                    amount = @Amount, 
                                    payment_method = @PaymentMethod, 
                                    payment_date = @PaymentDate,
                                    comment = @Comment
                                  WHERE id = @PaymentId;";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@PaymentId", payment.Id);
                        command.Parameters.AddWithValue("@CustomerId", payment.CustomerId);
                        command.Parameters.AddWithValue("@RentalId", payment.RentalId.HasValue ? (object)payment.RentalId.Value : DBNull.Value);
                        command.Parameters.AddWithValue("@Amount", Convert.ToInt32(payment.AmountPaid)); // Assuming DB 'amount' is INT
                        command.Parameters.AddWithValue("@PaymentMethod", payment.PaymentMethod);
                        command.Parameters.AddWithValue("@PaymentDate", payment.PaymentDate);
                        command.Parameters.AddWithValue("@Comment", payment.Comment);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (MySqlException ex) { Console.WriteLine($"MySQL Error updating payment {payment.Id}: {ex.Message}"); return false; }
            catch (Exception ex) { Console.WriteLine($"General Error updating payment {payment.Id}: {ex.Message}"); return false; }
        }

        public async Task<bool> DeletePaymentsByRentalIdAsync(int rentalId)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "DELETE FROM payment WHERE rental_id = @RentalId;";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@RentalId", rentalId);
                        await command.ExecuteNonQueryAsync();
                        return true; // Assume success even if no rows affected (no payments for that rental)
                    }
                }
            }
            catch (MySqlException ex) { Console.WriteLine($"MySQL Error deleting payments for rental {rentalId}: {ex.Message}"); return false; }
            catch (Exception ex) { Console.WriteLine($"General Error deleting payments for rental {rentalId}: {ex.Message}"); return false; }
        }


        public async Task<PaymentRecord?> GetInitialPaymentByRentalIdAsync(int rentalId)
        {
            PaymentRecord? payment = null;
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = @"SELECT id, customer_id, rental_id, amount, payment_method, payment_date 
                                  FROM payment WHERE rental_id = @RentalId ORDER BY payment_date ASC, id ASC LIMIT 1;";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@RentalId", rentalId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                payment = new PaymentRecord
                                {
                                    Id = reader.GetInt32("id"),
                                    CustomerId = reader.GetInt32("customer_id"),
                                    RentalId = reader.IsDBNull(reader.GetOrdinal("rental_id")) ? (int?)null : reader.GetInt32("rental_id"),
                                    AmountPaid = reader.IsDBNull(reader.GetOrdinal("amount")) ? 0 : Convert.ToDecimal(reader.GetInt32("amount")),
                                    PaymentMethod = reader.IsDBNull(reader.GetOrdinal("payment_method")) ? null : reader.GetString("payment_method"),
                                    PaymentDate = reader.GetDateTime("payment_date")
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"Error fetching initial payment for rental {rentalId}: {ex.Message}"); }
            return payment;
        }

        private void BuildPaymentWhereClause(StringBuilder whereBuilder, List<string> whereClauses, Dictionary<string, object?> parameters,
                                           DateTime? fromDate, DateTime? toDate, string? searchTerm, string? overviewPeriodFilter)
        {
            DateTime filterStartDate = DateTime.MinValue;
            DateTime filterEndDate = DateTime.MaxValue;
            bool usePeriodFilter = false;

            if (!string.IsNullOrEmpty(overviewPeriodFilter) && overviewPeriodFilter != "All Time") // Assuming "All Time" means no period filter
            {
                usePeriodFilter = true;
                DateTime today = DateTime.Today;
                switch (overviewPeriodFilter)
                {
                    case "Today":
                        filterStartDate = today;
                        filterEndDate = today;
                        break;
                    case "This Week":
                        // Assuming week starts on Monday for CultureInfo.CurrentCulture
                        int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                        filterStartDate = today.AddDays(-1 * diff).Date;
                        filterEndDate = filterStartDate.AddDays(6);
                        break;
                    case "This Month":
                        filterStartDate = new DateTime(today.Year, today.Month, 1);
                        filterEndDate = filterStartDate.AddMonths(1).AddDays(-1);
                        break;
                    case "This Year":
                        filterStartDate = new DateTime(today.Year, 1, 1);
                        filterEndDate = new DateTime(today.Year, 12, 31);
                        break;
                }
            }

            if (usePeriodFilter)
            {
                whereClauses.Add("p.payment_date >= @PeriodStartDate");
                parameters.Add("@PeriodStartDate", filterStartDate);
                whereClauses.Add("p.payment_date <= @PeriodEndDate");
                parameters.Add("@PeriodEndDate", filterEndDate.Date.AddDays(1).AddTicks(-1)); // Inclusive end date
            }
            else // Use manual date pickers if period filter is not active or "All Time"
            {
                if (fromDate.HasValue) { whereClauses.Add("p.payment_date >= @FromDate"); parameters.Add("@FromDate", fromDate.Value.Date); }
                if (toDate.HasValue) { whereClauses.Add("p.payment_date <= @ToDate"); parameters.Add("@ToDate", toDate.Value.Date.AddDays(1).AddTicks(-1)); }
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                whereClauses.Add("(c.name LIKE @SearchTerm OR p.id LIKE @SearchTerm OR IFNULL(r.id, '') LIKE @SearchTerm OR IFNULL(car.registration_number, '') LIKE @SearchTerm OR p.payment_method LIKE @SearchTerm)");
                parameters.Add("@SearchTerm", $"%{searchTerm}%");
            }

            if (whereClauses.Count > 0)
            {
                whereBuilder.Append(" WHERE ").Append(string.Join(" AND ", whereClauses));
            }
        }


        public async Task<List<PaymentDisplayRecord>> GetAllPaymentsAsync(
            int pageNumber = 1, int pageSize = 25,
            DateTime? fromDate = null, DateTime? toDate = null, string? searchTerm = null, string? overviewPeriodFilter = null)
        {
            var payments = new List<PaymentDisplayRecord>();
            var queryBuilder = new StringBuilder(@"
                SELECT p.id, p.amount, p.payment_method, p.payment_date, 
                       c.name AS ClientName,
                       r.id AS RentalIdForInfo, car.car_make, car.car_model, car.registration_number AS CarRegNo,
                       COALESCE(r.total_due, 0) AS RentalTotalDue
                FROM payment p
                JOIN customer c ON p.customer_id = c.id
                LEFT JOIN rental r ON p.rental_id = r.id
                LEFT JOIN car ON r.car_registration_no = car.registration_number
            ");
            var whereClauses = new List<string>();
            var parameters = new Dictionary<string, object?>();

            BuildPaymentWhereClause(queryBuilder, whereClauses, parameters, fromDate, toDate, searchTerm, overviewPeriodFilter);

            // The GROUP BY used previously for DueAmountOnRental via SUM(prev_p.amount) was overly complex and likely incorrect for a simple list.
            // We'll display RentalTotalDue directly. True outstanding due would be RentalTotalDue - SUM(all payments for that rental).
            queryBuilder.Append(" ORDER BY p.payment_date DESC, p.id DESC LIMIT @PageSize OFFSET @Offset;");
            parameters.Add("@PageSize", pageSize);
            parameters.Add("@Offset", (pageNumber - 1) * pageSize);

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new MySqlCommand(queryBuilder.ToString(), connection))
                    {
                        foreach (var p in parameters) command.Parameters.AddWithValue(p.Key, p.Value);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string rentalInfo = "N/A";
                                if (!reader.IsDBNull(reader.GetOrdinal("RentalIdForInfo")))
                                {
                                    rentalInfo = $"Rental #{reader.GetInt32("RentalIdForInfo")} - {reader.GetString("car_make")} {reader.GetString("car_model")} ({reader.GetString("CarRegNo")})";
                                }
                                payments.Add(new PaymentDisplayRecord
                                {
                                    Id = reader.GetInt32("id"),
                                    CustomerName = reader.GetString("ClientName"),
                                    AmountPaid = reader.IsDBNull(reader.GetOrdinal("amount")) ? 0 : Convert.ToDecimal(reader.GetInt32("amount")),
                                    DueAmountOnRental = reader.IsDBNull(reader.GetOrdinal("RentalTotalDue")) ? 0 : Convert.ToDecimal(reader.GetInt32("RentalTotalDue")),
                                    PaymentMethod = reader.IsDBNull(reader.GetOrdinal("payment_method")) ? null : reader.GetString("payment_method"),
                                    PaymentDate = reader.GetDateTime("payment_date"),
                                    RentalInfo = rentalInfo
                                });
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex) { Console.WriteLine($"MySQL Error fetching payments: {ex.Message}"); }
            catch (Exception ex) { Console.WriteLine($"General Error fetching payments: {ex.Message}"); }
            return payments;
        }

        public async Task<PaymentRecord?> GetPaymentByIdAsync(int paymentId)
        {
            PaymentRecord? payment = null;
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = @"SELECT id, customer_id, rental_id, amount, payment_method, payment_date, comment 
                          FROM payment WHERE id = @PaymentId;";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@PaymentId", paymentId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                payment = new PaymentRecord
                                {
                                    Id = reader.GetInt32("id"),
                                    CustomerId = reader.GetInt32("customer_id"),
                                    RentalId = reader.IsDBNull(reader.GetOrdinal("rental_id")) ? (int?)null : reader.GetInt32("rental_id"),
                                    AmountPaid = reader.IsDBNull(reader.GetOrdinal("amount")) ? 0 : reader.GetInt32("amount"),
                                    PaymentMethod = reader.IsDBNull(reader.GetOrdinal("payment_method")) ? null : reader.GetString("payment_method"),
                                    PaymentDate = reader.GetDateTime("payment_date"),
                                    Comment = reader.IsDBNull(reader.GetOrdinal("comment")) ? null : reader.GetString("comment")
                                };
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex) { Console.WriteLine($"MySQL Error fetching payment {paymentId}: {ex.Message}"); }
            catch (Exception ex) { Console.WriteLine($"General Error fetching payment {paymentId}: {ex.Message}"); }
            return payment;
        }


        public async Task<int> GetTotalPaymentCountAsync(
            DateTime? fromDate = null, DateTime? toDate = null, string? searchTerm = null, string? overviewPeriodFilter = null)
        {
            var queryBuilder = new StringBuilder(@"
                SELECT COUNT(DISTINCT p.id) 
                FROM payment p
                JOIN customer c ON p.customer_id = c.id
                LEFT JOIN rental r ON p.rental_id = r.id
                LEFT JOIN car ON r.car_registration_no = car.registration_number
            ");
            var whereClauses = new List<string>();
            var parameters = new Dictionary<string, object?>();
            BuildPaymentWhereClause(queryBuilder, whereClauses, parameters, fromDate, toDate, searchTerm, overviewPeriodFilter);
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new MySqlCommand(queryBuilder.ToString(), connection))
                    {
                        foreach (var p in parameters) command.Parameters.AddWithValue(p.Key, p.Value);
                        var result = await command.ExecuteScalarAsync();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (MySqlException ex) { Console.WriteLine($"MySQL Error getting total payment count: {ex.Message}"); return 0; }
            catch (Exception ex) { Console.WriteLine($"General Error getting total payment count: {ex.Message}"); return 0; }
        }
        public async Task<PaymentOverviewStats?> GetPaymentOverviewStatsAsync(DateTime filterFromDate, DateTime filterToDate)
        {
            var stats = new PaymentOverviewStats();

            DateTime startDate = filterFromDate.Date;
            DateTime endDate = filterToDate.Date.AddDays(1).AddTicks(-1); // Inclusive

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // Total Received Payments (from payment table)
                string queryReceived = @"SELECT COALESCE(SUM(amount), 0)
                                 FROM payment 
                                 WHERE payment_date >= @StartDate AND payment_date <= @EndDate;";
                using (var cmd = new MySqlCommand(queryReceived, connection))
                {
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endDate);
                    stats.TotalReceivedPayments = Convert.ToDecimal(await cmd.ExecuteScalarAsync());
                }

                // Total Due from Rentals (from rental table)
                string queryTotalDue = @"SELECT COALESCE(SUM(total_due), 0)
                                 FROM rental 
                                 WHERE start_date <= @EndDate AND end_date >= @StartDate;";
                using (var cmd = new MySqlCommand(queryTotalDue, connection))
                {
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endDate);
                    stats.TotalPayments = Convert.ToDecimal(await cmd.ExecuteScalarAsync());
                }

                // Remaining Due = Total Due - Received Payments
                stats.TotalOutstandingDueOnRentals = stats.TotalPayments - stats.TotalReceivedPayments;

                // Percentage Change from Last Period
                TimeSpan period = endDate - startDate;
                DateTime prevStart = startDate.AddDays(-period.TotalDays);
                DateTime prevEnd = endDate.AddDays(-period.TotalDays);

                string queryPrevPayments = @"SELECT COALESCE(SUM(amount), 0) 
                                     FROM payment 
                                     WHERE payment_date >= @PrevStart AND payment_date <= @PrevEnd;";
                using (var cmd = new MySqlCommand(queryPrevPayments, connection))
                {
                    cmd.Parameters.AddWithValue("@PrevStart", prevStart);
                    cmd.Parameters.AddWithValue("@PrevEnd", prevEnd);
                    decimal prevTotal = Convert.ToDecimal(await cmd.ExecuteScalarAsync());

                    stats.PercentageChangeFromLastPeriod = prevTotal == 0
                        ? 0
                        : Math.Round(((stats.TotalReceivedPayments - prevTotal) / prevTotal) * 100, 2);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting payment overview stats: {ex.Message}");
                return null;
            }

            return stats;
        }


        public async Task<int> GetTotalDueForCustomer(string cnic)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT IFNULL(SUM(total_due), 0) 
                  FROM rental 
                  WHERE customer_cnic = @cnic";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@cnic", cnic);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<int> GetRemainingDueForCustomer(string cnic)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
        SELECT 
            IFNULL(SUM(r.total_due), 0) - IFNULL(SUM(p.amount), 0) AS remaining_due
        FROM 
            rental r
        LEFT JOIN 
            payment p ON r.id = p.rental_id
        WHERE 
            r.customer_cnic = @cnic";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@cnic", cnic);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }


        public async Task<int> GetRemainingDueForRental(int rentalId, int customerId)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
        SELECT 
            IFNULL(r.total_due, 0) - IFNULL(SUM(p.amount), 0) AS remaining_due
        FROM 
            rental r
        LEFT JOIN 
            payment p ON r.id = p.rental_id
        WHERE 
            r.id = @rentalId AND r.customer_cnic = (
                SELECT cnic FROM customer WHERE id = @customerId
            )
        GROUP BY 
            r.total_due";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@rentalId", rentalId);
            cmd.Parameters.AddWithValue("@customerId", customerId);

            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }

    }
}