// NoorRAC/Services/MySQLRentalService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NoorRAC.Models;

namespace NoorRAC.Services
{
    public class MySQLRentalService : IRentalService // Implement the interface
    {
        // No database connection for this placeholder yet

        public async Task<List<RentalRecord>> GetRentalsByCustomerIdAsync(int customerId, string filter)
        {
            // Simulate async operation
            await Task.Delay(100); // Simulate network latency

            var rentals = new List<RentalRecord>();

            // Sample data based on customerId and filter
            if (customerId == 1) // e.g., Adan Ayaz
            {
                if (filter == "All time" || filter == "This month" || filter == "Previous week")
                {
                    rentals.Add(new RentalRecord { ID = 101, Name = "Toyota Corolla", CarNumber = "LHR-123", RentPerDay = 5000, Status = "Finished", StartDate = new DateTime(2024, 3, 1), EndDate = new DateTime(2024, 3, 5) });
                }
                if (filter == "All time" || filter == "This month")
                {
                    rentals.Add(new RentalRecord { ID = 102, Name = "Honda Civic", CarNumber = "KHI-789", RentPerDay = 6000, Status = "Active", StartDate = DateTime.Now.AddDays(-5), EndDate = DateTime.Now.AddDays(2) });
                }
                if (filter == "All time" || filter == "Previous week")
                {
                    rentals.Add(new RentalRecord { ID = 103, Name = "Suzuki Alto", CarNumber = "ISL-456", RentPerDay = 3000, Status = "Finished", StartDate = DateTime.Now.AddDays(-10), EndDate = DateTime.Now.AddDays(-7) });
                }
            }
            else if (customerId == 2) // e.g., Jane Smith
            {
                if (filter == "All time" || filter == "This month")
                {
                    rentals.Add(new RentalRecord { ID = 201, Name = "Kia Sportage", CarNumber = "PSH-001", RentPerDay = 7000, Status = "Active", StartDate = DateTime.Now.AddDays(-2), EndDate = DateTime.Now.AddDays(5) });
                }
            }
            // Add more sample data or logic based on the filter
            return rentals;
        }
    }
}