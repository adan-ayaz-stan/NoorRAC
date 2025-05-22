// NoorRAC/Services/MySQLRentalService.cs
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using NoorRAC.Models;

namespace NoorRAC.Services
{
    public class MySQLRentalService : IRentalService // Implement the interface
    {
        // No database connection for this placeholder yet
        private readonly string _connectionString = "server=localhost;database=NoorRAC;uid=root;pwd=root;";
        public async Task<int?> AddRentalAsync(RentalRecord rental, PaymentRecord? payment)
        {
            if (rental == null) throw new ArgumentNullException(nameof(rental));
            // Add validation for rental.CarId and rental.CustomerId existing if not handled by FK constraints robustly

            MySqlTransaction? transaction = null;
            int? newRentalId = null;

            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                transaction = await connection.BeginTransactionAsync();

                try
                {
                    // Get car_registration_no and customer_cnic from their IDs
                    // This assumes CarId and CustomerId are the integer PKs
                    string? carRegNo = null;
                    string? customerCnic = null;

                    // Fetch Car Registration Number
                    var carQuery = "SELECT registration_number FROM car WHERE id = @CarId;";
                    using (var carCmd = new MySqlCommand(carQuery, connection, transaction))
                    {
                        carCmd.Parameters.AddWithValue("@CarId", rental.CarId);
                        var result = await carCmd.ExecuteScalarAsync();
                        if (result != null) carRegNo = result.ToString();
                    }
                    if (string.IsNullOrEmpty(carRegNo)) throw new Exception($"Car with ID {rental.CarId} not found or has no registration number.");

                    // Fetch Customer CNIC
                    var custQuery = "SELECT cnic FROM customer WHERE id = @CustomerId;";
                    using (var custCmd = new MySqlCommand(custQuery, connection, transaction))
                    {
                        custCmd.Parameters.AddWithValue("@CustomerId", rental.CustomerId);
                        var result = await custCmd.ExecuteScalarAsync();
                        if (result != null) customerCnic = result.ToString();
                    }
                    if (string.IsNullOrEmpty(customerCnic)) throw new Exception($"Customer with ID {rental.CustomerId} not found or has no CNIC.");


                    // Insert Rental
                    var rentalQuery = @"INSERT INTO rental (start_date, end_date, rental_area, comments, total_due, car_registration_no, customer_cnic) 
                                        VALUES (@StartDate, @EndDate, @RentalArea, @Comments, @TotalDue, @CarRegNo, @CustomerCnic);
                                        SELECT LAST_INSERT_ID();"; // Get the ID of the inserted rental

                    using (var rentalCommand = new MySqlCommand(rentalQuery, connection, transaction))
                    {
                        rentalCommand.Parameters.AddWithValue("@StartDate", rental.StartDate);
                        rentalCommand.Parameters.AddWithValue("@EndDate", rental.EndDate);
                        rentalCommand.Parameters.AddWithValue("@RentalArea", rental.RentalArea ?? (object)DBNull.Value);
                        rentalCommand.Parameters.AddWithValue("@Comments", rental.OtherInformation ?? (object)DBNull.Value);
                        rentalCommand.Parameters.AddWithValue("@TotalDue", rental.TotalDue);
                        rentalCommand.Parameters.AddWithValue("@CarRegNo", carRegNo);
                        rentalCommand.Parameters.AddWithValue("@CustomerCnic", customerCnic);

                        var insertedId = await rentalCommand.ExecuteScalarAsync(); // Returns the new ID
                        if (insertedId != null && Convert.ToInt32(insertedId) > 0)
                        {
                            newRentalId = Convert.ToInt32(insertedId);
                        }
                        else
                        {
                            throw new Exception("Failed to insert rental record or retrieve its ID.");
                        }
                    }

                    // If payment is to be made now
                    if (payment != null && newRentalId.HasValue)
                    {
                        payment.RentalId = newRentalId.Value; // Link payment to the new rental
                        payment.CustomerId = rental.CustomerId; // Ensure CustomerId is also in payment

                        // You would typically have a separate Payments table
                        var paymentQuery = @"INSERT INTO payment (customer_id, rental_id, amount, payment_method, payment_date)
                                             VALUES (@CustomerId, @RentalId, @AmountPaid, @PaymentMethod, @PaymentDate);";
                        using (var paymentCommand = new MySqlCommand(paymentQuery, connection, transaction))
                        {
                            paymentCommand.Parameters.AddWithValue("@CustomerId", payment.CustomerId);
                            paymentCommand.Parameters.AddWithValue("@RentalId", payment.RentalId.Value);
                            paymentCommand.Parameters.AddWithValue("@AmountPaid", payment.AmountPaid);
                            paymentCommand.Parameters.AddWithValue("@PaymentMethod", payment.PaymentMethod ?? (object)DBNull.Value);
                            paymentCommand.Parameters.AddWithValue("@PaymentDate", payment.PaymentDate);

                            int paymentRowsAffected = await paymentCommand.ExecuteNonQueryAsync();
                            if (paymentRowsAffected == 0)
                            {
                                throw new Exception("Failed to insert payment record.");
                            }
                        }
                    }

                    await transaction.CommitAsync();
                    return newRentalId;
                }
                catch (Exception ex)
                {
                    await transaction?.RollbackAsync();
                    Console.WriteLine($"Error in AddRentalAsync: {ex.Message}");
                    // Log ex.ToString() for full details
                    return null; // Indicate failure
                }
            }
        }

        public async Task<List<RentalRecord>> GetRentalsByCustomerIdAsync(int customerId, string filter)
        {
            var rentals = new List<RentalRecord>();

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Get customer's CNIC using ID
                    string getCnicQuery = "SELECT cnic FROM customer WHERE id = @CustomerId;";
                    string? customerCnic = null;

                    using (var getCnicCmd = new MySqlCommand(getCnicQuery, connection))
                    {
                        getCnicCmd.Parameters.AddWithValue("@CustomerId", customerId);
                        var result = await getCnicCmd.ExecuteScalarAsync();
                        if (result == null) return rentals;
                        customerCnic = result.ToString();
                    }

                    // Build rental query
                    var queryBuilder = new StringBuilder(@"SELECT id, start_date, end_date, rental_area, comments, 
                                                   total_due, car_registration_no 
                                                   FROM rental 
                                                   WHERE customer_cnic = @CustomerCnic");

                    if (filter == "Active")
                    {
                        queryBuilder.Append(" AND end_date >= CURDATE()");
                    }
                    else if (filter == "Completed")
                    {
                        queryBuilder.Append(" AND end_date < CURDATE()");
                    }

                    queryBuilder.Append(" ORDER BY start_date DESC;");

                    using (var rentalCmd = new MySqlCommand(queryBuilder.ToString(), connection))
                    {
                        rentalCmd.Parameters.AddWithValue("@CustomerCnic", customerCnic);

                        using (var reader = await rentalCmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var rental = new RentalRecord
                                {
                                    ID = reader.GetInt32("id"),
                                    CustomerId = customerId,
                                    StartDate = reader.GetDateTime("start_date"),
                                    EndDate = reader.GetDateTime("end_date"),
                                    RentalArea = reader.IsDBNull("rental_area") ? null : reader.GetString("rental_area"),
                                    OtherInformation = reader.IsDBNull("comments") ? null : reader.GetString("comments"),
                                    TotalDue = reader.IsDBNull("total_due") ? 0 : Convert.ToDecimal(reader["total_due"]),
                                    Status = reader.GetDateTime("end_date") < DateTime.Today ? "Completed" : "Active"
                                };

                                // You can map CarId here if you have a way to resolve it from car_registration_no

                                rentals.Add(rental);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching rentals: {ex.Message}");
            }

            return rentals;
        }


        public async Task<List<RentalDisplayRecord>> GetAllRentalsForDisplayAsync(
            int pageNumber = 1, int pageSize = 25, string? searchTerm = null,
            DateTime? filterStartDate = null, DateTime? filterEndDate = null)
        {
            var rentals = new List<RentalDisplayRecord>();
            var queryBuilder = new StringBuilder(@"
                SELECT r.id AS RentalId, r.start_date, r.end_date, 
                       cust.name AS ClientName, 
                       car.car_make AS CarMake, car.car_model AS CarModel, 
                       r.car_registration_no AS CarRegistrationNumber 
                       -- Status will be calculated in C#
                FROM rental r
                JOIN customer cust ON r.customer_cnic = cust.cnic
                JOIN car ON r.car_registration_no = car.registration_number 
            "); // Note the JOIN conditions based on your schema

            var whereClauses = new List<string>();
            var parameters = new Dictionary<string, object?>();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                // Search on client name, car details, or rental ID
                whereClauses.Add("(cust.name LIKE @SearchTerm OR car.car_make LIKE @SearchTerm OR car.car_model LIKE @SearchTerm OR r.car_registration_no LIKE @SearchTerm OR r.id LIKE @SearchTerm)");
                parameters.Add("@SearchTerm", $"%{searchTerm}%");
            }
            if (filterStartDate.HasValue)
            {
                whereClauses.Add("r.start_date >= @FilterStartDate");
                parameters.Add("@FilterStartDate", filterStartDate.Value.Date);
            }
            if (filterEndDate.HasValue)
            {
                // If filtering by end date, usually you want rentals *ending* by this date
                // or rentals active *within* this date. Let's assume rentals ending on or before.
                whereClauses.Add("r.end_date <= @FilterEndDate");
                parameters.Add("@FilterEndDate", filterEndDate.Value.Date);
            }

            if (whereClauses.Count > 0)
            {
                queryBuilder.Append(" WHERE ").Append(string.Join(" AND ", whereClauses));
            }

            queryBuilder.Append(" ORDER BY r.start_date DESC, r.id DESC LIMIT @PageSize OFFSET @Offset;");
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
                                DateTime startDate = reader.GetDateTime("start_date");
                                DateTime endDate = reader.GetDateTime("end_date");
                                DateTime today = DateTime.Today; // Get current date once for comparison

                                string status;
                                if (endDate < today)
                                {
                                    status = "Finished";
                                }
                                else if (startDate <= today && endDate >= today)
                                {
                                    status = "Ongoing";
                                }
                                else // startDate > today
                                {
                                    status = "Scheduled";
                                }

                                rentals.Add(new RentalDisplayRecord
                                {
                                    Id = reader.GetInt32("RentalId"),
                                    ClientName = reader.GetString("ClientName"),
                                    CarType = $"{reader.GetString("CarMake")} {reader.GetString("CarModel")}",
                                    CarRegistrationNumber = reader.GetString("CarRegistrationNumber"),
                                    Status = status,
                                    StartDate = startDate,
                                    EndDate = endDate
                                });
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex) { Console.WriteLine($"MySQL Error fetching rentals for display: {ex.Message}"); }
            catch (Exception ex) { Console.WriteLine($"General Error fetching rentals for display: {ex.Message}"); }
            return rentals;
        }

        public async Task<int> GetTotalRentalCountAsync(
            string? searchTerm = null, DateTime? filterStartDate = null, DateTime? filterEndDate = null)
        {
            var queryBuilder = new StringBuilder(@"
                SELECT COUNT(r.id) 
                FROM rental r
                JOIN customer cust ON r.customer_cnic = cust.cnic
                JOIN car ON r.car_registration_no = car.registration_number
            ");
            var whereClauses = new List<string>();
            var parameters = new Dictionary<string, object?>();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                whereClauses.Add("(cust.name LIKE @SearchTerm OR car.car_make LIKE @SearchTerm OR car.car_model LIKE @SearchTerm OR r.car_registration_no LIKE @SearchTerm OR r.id LIKE @SearchTerm)");
                parameters.Add("@SearchTerm", $"%{searchTerm}%");
            }
            if (filterStartDate.HasValue)
            {
                whereClauses.Add("r.start_date >= @FilterStartDate");
                parameters.Add("@FilterStartDate", filterStartDate.Value.Date);
            }
            if (filterEndDate.HasValue)
            {
                whereClauses.Add("r.end_date <= @FilterEndDate");
                parameters.Add("@FilterEndDate", filterEndDate.Value.Date);
            }

            if (whereClauses.Count > 0)
            {
                queryBuilder.Append(" WHERE ").Append(string.Join(" AND ", whereClauses));
            }

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
            catch (MySqlException ex) { Console.WriteLine($"MySQL Error getting total rental count: {ex.Message}"); return 0; }
            catch (Exception ex) { Console.WriteLine($"General Error getting total rental count: {ex.Message}"); return 0; }
        }

        public async Task<RentalRecord?> GetRentalByIdAsync(int rentalId)
        {
            RentalRecord? rental = null;
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Fetch car_id and customer_id directly if your RentalRecord model uses them
                    // This query assumes rental table has car_id and customer_id (int)
                    // If it has car_registration_no and customer_cnic, you'll need to adjust
                    // or do a sub-query/join to get the int IDs if your model needs them.
                    // Let's assume RentalRecord uses CarId and CustomerId and the DB has them.
                    // If not, you need to adjust the query and mapping.
                    // Based on your DB schema: rental has car_registration_no and customer_cnic.
                    // So we fetch those, then look up CarId and CustomerId.
                    var query = @"
                SELECT r.id, r.start_date, r.end_date, r.rental_area, r.comments, r.total_due,
                       r.car_registration_no, r.customer_cnic,
                       c.id AS CarId, cust.id AS CustomerId 
                FROM rental r
                JOIN car c ON r.car_registration_no = c.registration_number
                JOIN customer cust ON r.customer_cnic = cust.cnic
                WHERE r.id = @RentalId;";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@RentalId", rentalId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                rental = new RentalRecord
                                {
                                    ID = reader.GetInt32("id"),
                                    StartDate = reader.GetDateTime("start_date"),
                                    EndDate = reader.GetDateTime("end_date"),
                                    RentalArea = reader.IsDBNull(reader.GetOrdinal("rental_area")) ? null : reader.GetString("rental_area"),
                                    OtherInformation = reader.IsDBNull(reader.GetOrdinal("comments")) ? null : reader.GetString("comments"),
                                    TotalDue = reader.IsDBNull(reader.GetOrdinal("total_due")) ? 0 : reader.GetDecimal("total_due"),
                                    CarId = reader.GetInt32("CarId"), // Assuming CarId is now fetched
                                    CustomerId = reader.GetInt32("CustomerId") // Assuming CustomerId is now fetched
                                                                               // Status would be calculated or loaded if stored
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"Error fetching rental {rentalId}: {ex.Message}"); }
            return rental;
        }

        public async Task<bool> UpdateRentalAsync(RentalRecord rental)
        {
            if (rental == null) throw new ArgumentNullException(nameof(rental));

            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // Get car_registration_no and customer_cnic from their IDs
                        string? carRegNo = null;
                        string? customerCnic = null;

                        var carQuery = "SELECT registration_number FROM car WHERE id = @CarId;";
                        using (var carCmd = new MySqlCommand(carQuery, connection, transaction))
                        {
                            carCmd.Parameters.AddWithValue("@CarId", rental.CarId);
                            var result = await carCmd.ExecuteScalarAsync();
                            if (result != null) carRegNo = result.ToString();
                        }
                        if (string.IsNullOrEmpty(carRegNo)) throw new Exception($"Car with ID {rental.CarId} not found.");

                        var custQuery = "SELECT cnic FROM customer WHERE id = @CustomerId;";
                        using (var custCmd = new MySqlCommand(custQuery, connection, transaction))
                        {
                            custCmd.Parameters.AddWithValue("@CustomerId", rental.CustomerId);
                            var result = await custCmd.ExecuteScalarAsync();
                            if (result != null) customerCnic = result.ToString();
                        }
                        if (string.IsNullOrEmpty(customerCnic)) throw new Exception($"Customer with ID {rental.CustomerId} not found.");

                        // Check for conflicts with other rentals of the same car
                        var conflictCheckQuery = @"
                    SELECT COUNT(*) FROM rental 
                    WHERE car_registration_no = @CarRegNo 
                    AND id != @RentalId 
                    AND (
                        (@StartDate BETWEEN start_date AND end_date) OR 
                        (@EndDate BETWEEN start_date AND end_date) OR
                        (start_date BETWEEN @StartDate AND @EndDate) OR
                        (end_date BETWEEN @StartDate AND @EndDate)
                    );";

                        using (var conflictCmd = new MySqlCommand(conflictCheckQuery, connection, transaction))
                        {
                            conflictCmd.Parameters.AddWithValue("@CarRegNo", carRegNo);
                            conflictCmd.Parameters.AddWithValue("@RentalId", rental.ID);
                            conflictCmd.Parameters.AddWithValue("@StartDate", rental.StartDate);
                            conflictCmd.Parameters.AddWithValue("@EndDate", rental.EndDate);

                            var conflictCount = Convert.ToInt32(await conflictCmd.ExecuteScalarAsync());

                            if (conflictCount > 0)
                            {
                                // Conflict found - throw exception with meaningful message
                                throw new InvalidOperationException(
                                    $"Cannot update rental dates because they conflict with another rental for the same car. " +
                                    $"The car '{carRegNo}' is already booked during part or all of the period from {rental.StartDate:yyyy-MM-dd} to {rental.EndDate:yyyy-MM-dd}.");
                            }
                        }

                        // If no conflicts, proceed with the update
                        var query = @"UPDATE rental SET 
                        start_date = @StartDate, end_date = @EndDate, rental_area = @RentalArea, 
                        comments = @Comments, total_due = @TotalDue, 
                        car_registration_no = @CarRegNo, customer_cnic = @CustomerCnic
                      WHERE id = @RentalId;";

                        using (var command = new MySqlCommand(query, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@RentalId", rental.ID);
                            command.Parameters.AddWithValue("@StartDate", rental.StartDate);
                            command.Parameters.AddWithValue("@EndDate", rental.EndDate);
                            command.Parameters.AddWithValue("@RentalArea", rental.RentalArea ?? (object)DBNull.Value);
                            command.Parameters.AddWithValue("@Comments", rental.OtherInformation ?? (object)DBNull.Value);
                            command.Parameters.AddWithValue("@TotalDue", rental.TotalDue);
                            command.Parameters.AddWithValue("@CarRegNo", carRegNo);
                            command.Parameters.AddWithValue("@CustomerCnic", customerCnic);

                            int rowsAffected = await command.ExecuteNonQueryAsync();
                            await transaction.CommitAsync();
                            return rowsAffected > 0;
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        // For expected exceptions like rental conflicts - return false and let the caller handle the message
                        await transaction.RollbackAsync();
                        Console.WriteLine($"Rental conflict detected for rental {rental.ID}: {ex.Message}");
                        throw; // Re-throw the exception so the UI can display the specific message
                    }
                    catch (Exception ex)
                    {
                        // For unexpected exceptions - rollback and log
                        await transaction.RollbackAsync();
                        Console.WriteLine($"Error updating rental {rental.ID}: {ex.Message}");
                        return false;
                    }
                }
            }
        }

        public async Task<bool> DeleteRentalAsync(int rentalId)
        {
            // ... (Implementation to delete rental and optionally its payments remains the same as before)
            MySqlTransaction? transaction = null;
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                transaction = await connection.BeginTransactionAsync();
                try
                {
                    var deletePaymentsQuery = "DELETE FROM payment WHERE rental_id = @RentalId;";
                    using (var paymentCmd = new MySqlCommand(deletePaymentsQuery, connection, transaction))
                    {
                        paymentCmd.Parameters.AddWithValue("@RentalId", rentalId);
                        await paymentCmd.ExecuteNonQueryAsync();
                    }
                    var query = "DELETE FROM rental WHERE id = @RentalId;";
                    using (var command = new MySqlCommand(query, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@RentalId", rentalId);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        await transaction.CommitAsync();
                        return rowsAffected > 0;
                    }
                }
                catch (Exception ex)
                {
                    await transaction?.RollbackAsync();
                    Console.WriteLine($"Error deleting rental {rentalId}: {ex.Message}");
                    return false;
                }
            }
        }
    }
}