// NoorRAC/Services/MySqlExpenseService.cs
using MySql.Data.MySqlClient;
using NoorRAC.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace NoorRAC.Services
{
    public class MySQLExpenseService : IExpenseService
    {
        private readonly string _connectionString = "server=localhost;database=NoorRAC;uid=root;pwd=root;"; // Replace with your actual connection string

        public async Task<bool> AddExpenseAsync(ExpenseRecord expense)
        {
            if (expense == null) throw new ArgumentNullException(nameof(expense));
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = @"INSERT INTO expense (date, amount, description, car_id) 
                                  VALUES (@Date, @Amount, @Description, @CarId);
                                  SELECT LAST_INSERT_ID();";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Date", expense.Date);
                        command.Parameters.AddWithValue("@Amount", expense.Amount); // INT
                        command.Parameters.AddWithValue("@Description", expense.Description);
                        command.Parameters.AddWithValue("@CarId", expense.CarId.HasValue ? (object)expense.CarId.Value : DBNull.Value);

                        var insertedId = await command.ExecuteScalarAsync();
                        return insertedId != null && Convert.ToInt32(insertedId) > 0;
                    }
                }
            }
            catch (MySqlException ex) { Console.WriteLine($"MySQL Error adding expense: {ex.Message}"); return false; }
            catch (Exception ex) { Console.WriteLine($"General Error adding expense: {ex.Message}"); return false; }
        }

        public async Task<bool> UpdateExpenseAsync(ExpenseRecord expense)
        {
            if (expense == null || expense.Id == 0) throw new ArgumentException("Expense or Expense ID is invalid for update.");
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = @"UPDATE expense SET 
                                    date = @Date, 
                                    amount = @Amount, 
                                    description = @Description,
                                    car_id = @CarId
                                  WHERE id = @ExpenseId;";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ExpenseId", expense.Id);
                        command.Parameters.AddWithValue("@Date", expense.Date);
                        command.Parameters.AddWithValue("@Amount", expense.Amount); // INT
                        command.Parameters.AddWithValue("@Description", expense.Description);
                        command.Parameters.AddWithValue("@CarId", expense.CarId.HasValue ? (object)expense.CarId.Value : DBNull.Value);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (MySqlException ex) { Console.WriteLine($"MySQL Error updating expense {expense.Id}: {ex.Message}"); return false; }
            catch (Exception ex) { Console.WriteLine($"General Error updating expense {expense.Id}: {ex.Message}"); return false; }
        }

        public async Task<bool> DeleteExpenseAsync(int expenseId)
        {
            if (expenseId <= 0) throw new ArgumentOutOfRangeException(nameof(expenseId));
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "DELETE FROM expense WHERE id = @ExpenseId;";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ExpenseId", expenseId);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (MySqlException ex) { Console.WriteLine($"MySQL Error deleting expense {expenseId}: {ex.Message}"); return false; }
            catch (Exception ex) { Console.WriteLine($"General Error deleting expense {expenseId}: {ex.Message}"); return false; }
        }

        public async Task<ExpenseRecord?> GetExpenseByIdAsync(int expenseId)
        {
            ExpenseRecord? expense = null;
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT id, date, amount, description, car_id FROM expense WHERE id = @ExpenseId;";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ExpenseId", expenseId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                expense = new ExpenseRecord
                                {
                                    Id = reader.GetInt32("id"),
                                    Date = reader.GetDateTime("date"),
                                    Amount = reader.IsDBNull(reader.GetOrdinal("amount")) ? 0 : reader.GetInt32("amount"),
                                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                                    CarId = reader.IsDBNull(reader.GetOrdinal("car_id")) ? (int?)null : reader.GetInt32("car_id")
                                };
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex) { Console.WriteLine($"MySQL Error fetching expense {expenseId}: {ex.Message}"); }
            catch (Exception ex) { Console.WriteLine($"General Error fetching expense {expenseId}: {ex.Message}"); }
            return expense;
        }

        private void BuildExpenseWhereClause(StringBuilder whereBuilder, List<string> whereClauses, Dictionary<string, object?> parameters,
                                          DateTime? fromDate, DateTime? toDate, string? searchTerm)
        {
            if (fromDate.HasValue)
            {
                whereClauses.Add("e.date >= @FromDate");
                parameters.Add("@FromDate", fromDate.Value.Date);
            }
            if (toDate.HasValue)
            {
                whereClauses.Add("e.date <= @ToDate");
                parameters.Add("@ToDate", toDate.Value.Date.AddDays(1).AddTicks(-1)); // Inclusive end date
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                // Search in description, amount (if numeric), or car registration number
                whereClauses.Add("(e.description LIKE @SearchTerm OR " +
                                 "CAST(e.amount AS CHAR) LIKE @SearchTerm OR " + // Search amount as string
                                 "c.registration_number LIKE @SearchTerm OR " +
                                 "c.car_make LIKE @SearchTerm OR " +
                                 "c.car_model LIKE @SearchTerm)");
                parameters.Add("@SearchTerm", $"%{searchTerm}%");
            }

            if (whereClauses.Count > 0)
            {
                whereBuilder.Append(" WHERE ").Append(string.Join(" AND ", whereClauses));
            }
        }

        public async Task<List<ExpenseDisplayRecord>> GetAllExpensesAsync(
            int pageNumber = 1, int pageSize = 25,
            DateTime? fromDate = null, DateTime? toDate = null, string? searchTerm = null)
        {
            var expenses = new List<ExpenseDisplayRecord>();
            var queryBuilder = new StringBuilder(@"
                SELECT e.id, e.date, e.amount, e.description, e.car_id,
                       c.registration_number AS CarRegNo, c.car_make AS CarMake, c.car_model AS CarModel
                FROM expense e
                LEFT JOIN car c ON e.car_id = c.id 
            "); // Assuming car table has 'id' as PK, 'registration_number', 'car_make', 'car_model'
            var whereClauses = new List<string>();
            var parameters = new Dictionary<string, object?>();

            BuildExpenseWhereClause(queryBuilder, whereClauses, parameters, fromDate, toDate, searchTerm);

            queryBuilder.Append(" ORDER BY e.date DESC, e.id DESC LIMIT @PageSize OFFSET @Offset;");
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
                                string carInfo = "N/A";
                                int? carId = reader.IsDBNull(reader.GetOrdinal("car_id")) ? (int?)null : reader.GetInt32("car_id");
                                if (carId.HasValue && !reader.IsDBNull(reader.GetOrdinal("CarRegNo")))
                                {
                                    carInfo = $"Car: {reader.GetString("CarRegNo")}";
                                    if (!reader.IsDBNull(reader.GetOrdinal("CarMake"))) carInfo += $" ({reader.GetString("CarMake")}";
                                    if (!reader.IsDBNull(reader.GetOrdinal("CarModel"))) carInfo += $" {reader.GetString("CarModel")}";
                                    if (!reader.IsDBNull(reader.GetOrdinal("CarMake"))) carInfo += ")"; // Closing parenthesis if make was present
                                }

                                expenses.Add(new ExpenseDisplayRecord
                                {
                                    Id = reader.GetInt32("id"),
                                    Date = reader.GetDateTime("date"),
                                    Amount = reader.IsDBNull(reader.GetOrdinal("amount")) ? 0 : reader.GetInt32("amount"),
                                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                                    CarInfo = carInfo,
                                    CarId = carId
                                });
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex) { Console.WriteLine($"MySQL Error fetching expenses: {ex.Message}"); }
            catch (Exception ex) { Console.WriteLine($"General Error fetching expenses: {ex.Message}"); }
            return expenses;
        }

        public async Task<int> GetTotalExpensesCountAsync(
            DateTime? fromDate = null, DateTime? toDate = null, string? searchTerm = null)
        {
            var queryBuilder = new StringBuilder(@"
                SELECT COUNT(DISTINCT e.id) 
                FROM expense e
                LEFT JOIN car c ON e.car_id = c.id
            ");
            var whereClauses = new List<string>();
            var parameters = new Dictionary<string, object?>();

            BuildExpenseWhereClause(queryBuilder, whereClauses, parameters, fromDate, toDate, searchTerm);

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
            catch (MySqlException ex) { Console.WriteLine($"MySQL Error getting total expense count: {ex.Message}"); return 0; }
            catch (Exception ex) { Console.WriteLine($"General Error getting total expense count: {ex.Message}"); return 0; }
        }
    }
}