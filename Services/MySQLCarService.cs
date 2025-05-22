// NoorRAC/Services/MySQLCarService.cs (or PlaceholderCarService.cs)
using MySql.Data.MySqlClient;
using NoorRAC.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace NoorRAC.Services
{
    public class MySQLCarService : ICarService // Or PlaceholderCarService
    {
        private readonly string _connectionString = "server=localhost;database=NoorRAC;uid=root;pwd=root;";

        public async Task<bool> AddCarAsync(Car car)
        {
            // ... (implementation as before) ...
            if (car == null) throw new ArgumentNullException(nameof(car));
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = @"INSERT INTO car (registration_number, car_model, car_make, rent_per_day, owner_name, owner_cnic, owner_lending_fees, owner_phone) 
                                  VALUES (@RegistrationNumber, @CarModel, @CarMake, @RentPerDay, @OwnerName, @OwnerCNIC, @OwnerLendingFees, @OwnerPhone);";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@RegistrationNumber", car.RegistrationNumber);
                        command.Parameters.AddWithValue("@CarModel", car.CarModel);
                        command.Parameters.AddWithValue("@CarMake", car.CarMake);
                        command.Parameters.AddWithValue("@RentPerDay", car.RentPerDay);
                        command.Parameters.AddWithValue("@OwnerName", car.OwnerName);
                        command.Parameters.AddWithValue("@OwnerCNIC", car.OwnerCNIC);
                        command.Parameters.AddWithValue("@OwnerLendingFees", car.OwnerLendingFees);
                        command.Parameters.AddWithValue("@OwnerPhone", car.OwnerPhone ?? (object)DBNull.Value);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (MySqlException ex) { Console.WriteLine($"MySQL Error adding car: {ex.Message}"); return false; }
            catch (Exception ex) { Console.WriteLine($"General Error adding car: {ex.Message}"); return false; }
        }


        public async Task<List<Car>> GetAvailableCarsAsync(DateTime startDate, DateTime endDate)
        {
            var availableCars = new List<Car>();
            // Basic validation for dates
            if (startDate.Date >= endDate.Date)
            {
                // Or throw an ArgumentException, or return empty list silently
                Console.WriteLine("GetAvailableCarsAsync: Start date must be before end date.");
                return availableCars; // Return empty list if dates are invalid
            }

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Query to find cars that DO NOT have any rentals overlapping with the given [startDate, endDate] period.
                    // A car is considered NOT available if there's a rental for it where:
                    // (rental.start_date < @DesiredEndDate) AND (rental.end_date > @DesiredStartDate)
                    // Note: Using car.id to link to rental.car_id if rental table uses car_id (int)
                    // If rental table uses car_registration_no, join on that.
                    // Your rental table uses `car_registration_no`.
                    var query = @"
                        SELECT c.id, c.registration_number, c.car_model, c.car_make,
                               c.rent_per_day, c.owner_name, c.owner_cnic, c.owner_lending_fees
                        FROM car c
                        WHERE NOT EXISTS (
                            SELECT 1
                            FROM rental r
                            WHERE r.car_registration_no = c.registration_number
                            AND r.start_date < @DesiredEndDate  -- Existing rental starts before desired one ends
                            AND r.end_date > @DesiredStartDate  -- Existing rental ends after desired one starts
                        )
                        ORDER BY c.car_make, c.car_model;";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        // Pass only the Date part if your DB stores dates without time,
                        // or if you want to compare based on full days.
                        command.Parameters.AddWithValue("@DesiredStartDate", startDate.Date);
                        command.Parameters.AddWithValue("@DesiredEndDate", endDate.Date);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                availableCars.Add(new Car
                                {
                                    Id = reader.GetInt32("id"),
                                    RegistrationNumber = reader.GetString("registration_number"),
                                    CarModel = reader.GetString("car_model"),
                                    CarMake = reader.GetString("car_make"),
                                    RentPerDay = reader.GetInt32("rent_per_day"),
                                    OwnerName = reader.GetString("owner_name"),
                                    OwnerCNIC = reader.IsDBNull(reader.GetOrdinal("owner_cnic")) ? string.Empty : reader.GetString("owner_cnic"),
                                    OwnerLendingFees = reader.GetInt32("owner_lending_fees")
                                });
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"MySQL Error fetching available cars for period {startDate:d} - {endDate:d}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error fetching available cars for period {startDate:d} - {endDate:d}: {ex.Message}");
            }
            return availableCars;
        }

        public async Task<List<Car>> GetAllCarsWithDetailsAsync()
        {
            var cars = new List<Car>();
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Select all columns from the 'car' table
                    // You might want to add pagination here later if the car list becomes very large
                    var query = @"SELECT id, registration_number, car_model, car_make, 
                                         rent_per_day, owner_name, owner_cnic, owner_lending_fees 
                                  FROM car
                                  ORDER BY car_make, car_model;"; // Example ordering

                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var car = new Car
                                {
                                    Id = reader.GetInt32("id"),
                                    RegistrationNumber = reader.GetString("registration_number"),
                                    CarModel = reader.GetString("car_model"),
                                    CarMake = reader.GetString("car_make"),
                                    RentPerDay = reader.GetInt32("rent_per_day"),
                                    OwnerName = reader.GetString("owner_name"),
                                    OwnerCNIC = reader.IsDBNull(reader.GetOrdinal("owner_cnic")) ? string.Empty : reader.GetString("owner_cnic"),
                                    OwnerLendingFees = reader.GetInt32("owner_lending_fees")
                                };
                                cars.Add(car);
                            }
                        } // Reader is disposed
                    }
                } // Connection is disposed
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"MySQL Error fetching all cars with details: {ex.Message}");
                // Optionally, rethrow or handle more gracefully
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error fetching all cars with details: {ex.Message}");
            }

            // At this point, 'cars' contains the basic details from the 'car' table.
            // The rental-derived properties (status, total rentals, income, etc.)
            // would need to be populated here by querying the 'rental' table for each car,
            // or by a more complex initial JOIN query.
            // For now, they will have their default values as per the Car model.

            return cars;
        }

        public async Task<CarOverviewStats?> GetCarOverviewStatsAsync()
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Get total cars
                    int totalCars = 0;
                    using (var totalCmd = new MySqlCommand("SELECT COUNT(*) FROM car;", connection))
                    {
                        var result = await totalCmd.ExecuteScalarAsync();
                        totalCars = Convert.ToInt32(result);
                    }

                    // Get cars currently rented (assuming a 'rental' table with car_id, start_date, end_date)
                    int carsRented = 0;
                    using (var rentedCmd = new MySqlCommand(
                        @"SELECT COUNT(DISTINCT car_registration_no) 
                  FROM rental 
                  WHERE end_date >= CURDATE() AND start_date <= CURDATE();", connection))
                    {
                        var result = await rentedCmd.ExecuteScalarAsync();
                        carsRented = Convert.ToInt32(result);
                    }

                    int carsAvailable = totalCars - carsRented;

                    return new CarOverviewStats
                    {
                        TotalCars = totalCars,
                        CarsRented = carsRented,
                        CarsAvailable = carsAvailable
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting car overview stats: {ex.Message}");
                return null;
            }
        }


        public async Task<Car?> GetCarByIdAsync(int id)
        {
            Car? car = null;
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT id, registration_number, car_model, car_make, rent_per_day, owner_name, owner_cnic, owner_lending_fees, owner_phone FROM car WHERE id = @Id;";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                car = new Car
                                {
                                    Id = reader.GetInt32("id"),
                                    RegistrationNumber = reader.GetString("registration_number"),
                                    CarModel = reader.GetString("car_model"),
                                    CarMake = reader.GetString("car_make"),
                                    RentPerDay = reader.GetInt32("rent_per_day"),
                                    OwnerName = reader.GetString("owner_name"),
                                    OwnerCNIC = reader.IsDBNull(reader.GetOrdinal("owner_cnic")) ? string.Empty : reader.GetString("owner_cnic"),
                                    OwnerLendingFees = reader.GetInt32("owner_lending_fees"),
                                    OwnerPhone = reader.IsDBNull(reader.GetOrdinal("owner_phone")) ? string.Empty : reader.GetString("owner_phone")
                                };
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex) { Console.WriteLine($"MySQL Error fetching car by ID {id}: {ex.Message}"); }
            catch (Exception ex) { Console.WriteLine($"General Error fetching car by ID {id}: {ex.Message}"); }
            return car;
        }

        public async Task<bool> UpdateCarAsync(Car car)
        {
            if (car == null) throw new ArgumentNullException(nameof(car));
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = @"UPDATE car 
                                  SET registration_number = @RegistrationNumber, 
                                      car_model = @CarModel, 
                                      car_make = @CarMake, 
                                      rent_per_day = @RentPerDay, 
                                      owner_name = @OwnerName,
                                      owner_cnic = @OwnerCNIC,
                                      owner_lending_fees = @OwnerLendingFees,
                                      owner_phone = @OwnerPhone
                                  WHERE id = @Id;";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", car.Id);
                        command.Parameters.AddWithValue("@RegistrationNumber", car.RegistrationNumber);
                        command.Parameters.AddWithValue("@CarModel", car.CarModel);
                        command.Parameters.AddWithValue("@CarMake", car.CarMake);
                        command.Parameters.AddWithValue("@RentPerDay", car.RentPerDay);
                        command.Parameters.AddWithValue("@OwnerName", car.OwnerName);
                        command.Parameters.AddWithValue("@OwnerCNIC", car.OwnerCNIC ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@OwnerLendingFees", car.OwnerLendingFees);
                        command.Parameters.AddWithValue("@OwnerPhone", car.OwnerPhone ?? (object)DBNull.Value);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (MySqlException ex) { Console.WriteLine($"MySQL Error updating car {car.Id}: {ex.Message}"); return false; }
            catch (Exception ex) { Console.WriteLine($"General Error updating car {car.Id}: {ex.Message}"); return false; }
        }

        public async Task<bool> DeleteCarAsync(int id)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Add logic here to check if car is part of any active/future rentals before deleting
                    // For now, just deleting:
                    var query = "DELETE FROM car WHERE id = @Id;";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (MySqlException ex) { Console.WriteLine($"MySQL Error deleting car {id}: {ex.Message}"); return false; }
            catch (Exception ex) { Console.WriteLine($"General Error deleting car {id}: {ex.Message}"); return false; }
        }

    }
}