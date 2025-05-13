using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient; // Or using MySqlConnector;
using NoorRAC.Models;
using System;
using System.Threading.Tasks;
using System.Data;
// using Microsoft.Extensions.Configuration; // To get connection string

namespace NoorRAC.Services
{
    public class MySqlAuthenticationService : IAuthenticationService
    {
        // --- OR Hardcode for simplicity (Not recommended for production) ---
        private readonly string _connectionString = "server=localhost;database=NoorRAC;uid=root;pwd=root;";


        public async Task<User?> LoginAsync(string username, string password)
        {
            User? user = null;
            // IMPORTANT: Hash the provided password using the SAME algorithm and salt (if used)
            // as when the user was created/password was set.
            // Example using a simple hypothetical hashing function:
            // string hashedPassword = HashPassword(password);

            // --- Database Logic ---
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT id, username, password FROM user_admin WHERE username = @Username"; // LIMIT 1 is good practice
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);
                        // command.Parameters.AddWithValue("@PasswordHash", hashedPassword); // Compare hash in DB query *if secure*

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                // IMPORTANT: Verify the HASH here, not in the SQL query ideally.
                                string storedHash = reader.GetString("password");
                                if (VerifyPasswordHash(password, storedHash)) // Implement this verification
                                {
                                    user = new User
                                    {
                                        Id = reader.GetInt32("id"),
                                        Username = reader.GetString("username"),
                                        PasswordHash = storedHash // Or don't store it in the logged-in user object
                                    };
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Database login error: {ex.Message}");
                // Optionally re-throw or handle differently
            }
            return user;
        }

        // --- Password Hashing Placeholder ---
        // Use a strong library like BCrypt.Net or ASP.NET Core Identity's PasswordHasher
        private bool VerifyPasswordHash(string providedPassword, string storedHash)
        {
            // Replace with actual hash verification logic  
            // Example using BCrypt.Net: return BCrypt.Net.BCrypt.Verify(providedPassword, storedHash);  
            return BCrypt.Net.BCrypt.Verify(providedPassword, storedHash);
        }
    }
}