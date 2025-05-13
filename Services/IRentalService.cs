// NoorRAC/Services/IRentalService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using NoorRAC.Models; // For RentalRecord

namespace NoorRAC.Services
{
    public interface IRentalService
    {
        /// <summary>
        /// Gets all rental records for a specific customer, optionally filtered.
        /// </summary>
        /// <param name="customerId">The ID of the customer.</param>
        /// <param name="filter">A string representing the filter (e.g., "This month", "All time").</param>
        /// <returns>A list of rental records for the customer.</returns>
        Task<List<RentalRecord>> GetRentalsByCustomerIdAsync(int customerId, string filter);

        // You might add other rental-related methods here later
    }
}