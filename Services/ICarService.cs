// NoorRAC/Services/ICarService.cs
using NoorRAC.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NoorRAC.Services
{
    public interface ICarService
    {
        Task<bool> AddCarAsync(Car car);
        Task<List<Car>> GetAvailableCarsAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets all cars, potentially with some summary status or rental info.
        /// For simplicity, this might just return List<Car> and the ViewModel calculates status.
        /// Or, it could return a more complex DTO if the query is optimized.
        /// </summary>
        Task<List<Car>> GetAllCarsWithDetailsAsync(); // New or modify existing get all

        // Placeholder for overview stats for the Cars view
        Task<CarOverviewStats?> GetCarOverviewStatsAsync();

        /// <summary>
        /// Gets a specific car by its ID.
        /// </summary>
        /// <param name="id">The ID of the car to retrieve.</param>
        /// <returns>The car record if found; otherwise, null.</returns>
        Task<Car?> GetCarByIdAsync(int id); // New or ensure it exists

        /// <summary>
        /// Updates an existing car's information.
        /// </summary>
        /// <param name="car">The car data with updates.</param>
        /// <returns>True if the update was successful; otherwise, false.</returns>
        Task<bool> UpdateCarAsync(Car car); // New or ensure it exists

        /// <summary>
        /// Deletes a car by its ID.
        /// </summary>
        /// <param name="id">The ID of the car to delete.</param>
        /// <returns>True if deletion was successful; otherwise, false.</returns>
        Task<bool> DeleteCarAsync(int id); // New or ensure it exists
    }

    public class CarOverviewStats // Helper class for stats
    {
        public int TotalCars { get; set; }
        public int CarsRented { get; set; }
        public int CarsAvailable { get; set; }
    }
}