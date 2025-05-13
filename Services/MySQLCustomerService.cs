using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using NoorRAC.Models;

namespace NoorRAC.Services
{
    internal class MySQLCustomerService : ICustomerService
    {
        private readonly string _connectionString = "server=localhost;database=NoorRAC;uid=root;pwd=root;";

        public async Task<List<CustomerRecord>> GetAllCustomersAsync(int pageNumber = 1, int pageSize = 25, string? searchTerm = null)
        {
            var customers = new List<CustomerRecord>();
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var queryBuilder = new StringBuilder("SELECT id, cnic, name, address, contact, date_joined FROM customer");
                    var parameters = new Dictionary<string, object>();

                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        queryBuilder.Append(" WHERE name LIKE @SearchTerm OR cnic LIKE @SearchTerm OR contact LIKE @SearchTerm OR address LIKE @SearchTerm");
                        parameters.Add("@SearchTerm", $"%{searchTerm}%");
                    }

                    queryBuilder.Append(" ORDER BY name LIMIT @PageSize OFFSET @Offset;");
                    parameters.Add("@PageSize", pageSize);
                    parameters.Add("@Offset", (pageNumber - 1) * pageSize);

                    using (var command = new MySqlCommand(queryBuilder.ToString(), connection))
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value);
                        }

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                customers.Add(new CustomerRecord
                                {
                                    ID = reader.GetInt32("id"),
                                    CNIC = reader.GetString("cnic"),
                                    Name = reader.GetString("name"),
                                    Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString("address"),
                                    ContactInfo = reader.IsDBNull(reader.GetOrdinal("contact")) ? null : reader.GetString("contact"),
                                    DateJoined = reader.IsDBNull(reader.GetOrdinal("date_joined")) ? DateTime.MinValue : reader.GetDateTime("date_joined")
                                });
                            }
                        }
                    }

                    // Rental summary fetching (N+1) remains the same for the paged/filtered customers
                    foreach (var customer in customers)
                    {
                        var summaryQuery = @"SELECT COUNT(*) AS TotalRentals, SUM(total_due) AS TotalOutstandingPayments
                                             FROM rental
                                             WHERE customer_id = @CustomerId;";
                        using (var summaryCommand = new MySqlCommand(summaryQuery, connection))
                        {
                            summaryCommand.Parameters.AddWithValue("@CustomerId", customer.ID);
                            using (var summaryReader = await summaryCommand.ExecuteReaderAsync())
                            {
                                if (await summaryReader.ReadAsync())
                                {
                                    customer.TotalRentals = summaryReader.GetInt32("TotalRentals");
                                    customer.OutstandingPayments = summaryReader.IsDBNull(summaryReader.GetOrdinal("TotalOutstandingPayments")) ? 0m : summaryReader.GetDecimal("TotalOutstandingPayments");
                                }
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex) { Console.WriteLine($"MySQL Error fetching customers: {ex.Message}"); }
            catch (Exception ex) { Console.WriteLine($"General Error fetching customers: {ex.Message}"); }
            return customers;
        }

        public async Task<int> GetTotalCustomerCountAsync(string? searchTerm = null)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var queryBuilder = new StringBuilder("SELECT COUNT(*) FROM customer");
                    MySqlCommand command;

                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        queryBuilder.Append(" WHERE name LIKE @SearchTerm OR cnic LIKE @SearchTerm OR contact LIKE @SearchTerm OR address LIKE @SearchTerm");
                        command = new MySqlCommand(queryBuilder.ToString(), connection);
                        command.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");
                    }
                    else
                    {
                        command = new MySqlCommand(queryBuilder.ToString(), connection);
                    }

                    using (command) // Ensure command is disposed
                    {
                        var result = await command.ExecuteScalarAsync();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (MySqlException ex) { Console.WriteLine($"MySQL Error getting total customer count: {ex.Message}"); return 0; }
            catch (Exception ex) { Console.WriteLine($"General Error getting total customer count: {ex.Message}"); return 0; }
        }

        public async Task<bool> AddCustomerAsync(CustomerRecord customer)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = @"INSERT INTO customer (cnic, name, address, contact, date_joined) 
                                  VALUES (@Cnic, @Name, @Address, @Contact, @DateJoined);";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Cnic", customer.CNIC);
                        command.Parameters.AddWithValue("@Name", customer.Name);
                        command.Parameters.AddWithValue("@Address", customer.Address ?? "");
                        command.Parameters.AddWithValue("@Contact", customer.ContactInfo ?? "");
                        command.Parameters.AddWithValue("@DateJoined", customer.DateJoined);

                        await command.ExecuteNonQueryAsync();
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving customer: {ex.Message}");
                return false;
            }
        }

        public async Task<CustomerRecord?> GetCustomerByIdAsync(int id)
        {
            CustomerRecord? customer = null;
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Fetch basic customer details
                    var query = "SELECT id, cnic, name, address, contact, date_joined FROM customer WHERE id = @Id;";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                customer = new CustomerRecord
                                {
                                    ID = reader.GetInt32("id"),
                                    CNIC = reader.GetString("cnic"),
                                    Name = reader.GetString("name"),
                                    Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString("address"),
                                    ContactInfo = reader.IsDBNull(reader.GetOrdinal("contact")) ? null : reader.GetString("contact"),
                                    DateJoined = reader.IsDBNull(reader.GetOrdinal("date_joined")) ? DateTime.MinValue : reader.GetDateTime("date_joined")
                                };
                            }
                        } // Reader disposed
                    }

                    if (customer != null)
                    {
                        // Fetch rental summary for this specific customer
                        var summaryQuery = @"SELECT COUNT(*) AS TotalRentals, SUM(total_due) AS TotalOutstandingPayments
                                             FROM rental
                                             WHERE customer_id = @CustomerId;";
                        using (var summaryCommand = new MySqlCommand(summaryQuery, connection))
                        {
                            summaryCommand.Parameters.AddWithValue("@CustomerId", customer.ID);
                            using (var summaryReader = await summaryCommand.ExecuteReaderAsync())
                            {
                                if (await summaryReader.ReadAsync())
                                {
                                    customer.TotalRentals = summaryReader.GetInt32("TotalRentals");
                                    customer.OutstandingPayments = summaryReader.IsDBNull(summaryReader.GetOrdinal("TotalOutstandingPayments")) ? 0m : summaryReader.GetDecimal("TotalOutstandingPayments");
                                }
                            } // SummaryReader disposed
                        }
                    }
                } // Connection disposed
            }
            catch (MySqlException ex) { Console.WriteLine($"MySQL Error fetching customer by ID {id}: {ex.Message}"); }
            catch (Exception ex) { Console.WriteLine($"General Error fetching customer by ID {id}: {ex.Message}"); }
            return customer;
        }

        public async Task<bool> UpdateCustomerAsync(CustomerRecord customer)
        {
            if (customer == null) throw new ArgumentNullException(nameof(customer));
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = @"UPDATE customer 
                                  SET cnic = @Cnic, name = @Name, address = @Address, contact = @Contact, date_joined = @DateJoined
                                  WHERE id = @Id;";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", customer.ID);
                        command.Parameters.AddWithValue("@Cnic", customer.CNIC);
                        command.Parameters.AddWithValue("@Name", customer.Name);
                        command.Parameters.AddWithValue("@Address", customer.Address ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Contact", customer.ContactInfo ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@DateJoined", customer.DateJoined); // Assuming date_joined can be updated

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (MySqlException ex) { Console.WriteLine($"MySQL Error updating customer {customer.ID}: {ex.Message}"); return false; }
            catch (Exception ex) { Console.WriteLine($"General Error updating customer {customer.ID}: {ex.Message}"); return false; }
        }

        public async Task<bool> DeleteCustomerAsync(int id)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Consider what to do with related rentals:
                    // 1. Delete them (cascade delete in DB or separate DELETE statements)
                    // 2. Set rental.customer_id to NULL (if allowed)
                    // 3. Prevent deletion if rentals exist
                    // For now, just deleting the customer:
                    var query = "DELETE FROM customer WHERE id = @Id;";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (MySqlException ex) { Console.WriteLine($"MySQL Error deleting customer {id}: {ex.Message}"); return false; }
            catch (Exception ex) { Console.WriteLine($"General Error deleting customer {id}: {ex.Message}"); return false; }
        }
    }
}
