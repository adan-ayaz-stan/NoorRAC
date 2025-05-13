using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient; // Or using MySqlConnector;
using NoorRAC.ViewModels; // Or NoorRAC.Models
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NoorRAC.Services
{
    public class MySqlCarService : ICarService
    {
        private readonly string _connectionString;

        public MySqlCarService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                 ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<int> GetAvailableCountAsync()
        {
            int count = 0;
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // Cars NOT in an active rental (CURDATE() between start/end)
                var query = @"
                    SELECT COUNT(c.id)
                    FROM car c
                    LEFT JOIN rental r ON c.id = r.car_id AND CURDATE() BETWEEN r.start_date AND r.end_date
                    WHERE r.id IS NULL;"; // Only count cars where the join condition didn't match an active rental

                using var command = new MySqlCommand(query, connection);
                var result = await command.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    count = Convert.ToInt32(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAvailableCountAsync: {ex.Message}");
                // Handle or log error appropriately
            }
            return count;
        }

        public async Task<IEnumerable<CarSummaryViewModel>> GetRecentAvailableSummaryAsync(int count)
        {
            var cars = new List<CarSummaryViewModel>();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // Select available cars (using LEFT JOIN method) and join to get make/model
                var query = @"
                    SELECT c.registration_number, cmk.make_name, cmd.model_name
                    FROM car c
                    JOIN car_model cmd ON c.model_id = cmd.id
                    JOIN car_make cmk ON cmd.make_id = cmk.id
                    LEFT JOIN rental r ON c.id = r.car_id AND CURDATE() BETWEEN r.start_date AND r.end_date
                    WHERE r.id IS NULL
                    ORDER BY c.id DESC
                    LIMIT @Count;";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Count", count);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    cars.Add(new CarSummaryViewModel
                    {
                        RegNumber = reader["registration_number"] as string,
                        MakeModel = $"{reader["make_name"]} {reader["model_name"]}"
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetRecentAvailableSummaryAsync: {ex.Message}");
                // Handle or log error appropriately
            }
            return cars;
        }
    }
}