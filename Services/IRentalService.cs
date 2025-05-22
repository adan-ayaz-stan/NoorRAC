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
        /// <summary>
        /// Adds a new rental and, optionally, its associated initial payment.
        /// </summary>
        /// <param name="rental">The rental record to add.</param>
        /// <param name="payment">The initial payment record (can be null if no immediate payment).</param>
        /// <returns>The ID of the newly created rental if successful, otherwise null or 0.</returns>
        Task<int?> AddRentalAsync(RentalRecord rental, PaymentRecord? payment); // Updated signature

        /// <summary>
        /// Gets a paginated list of all rentals for display, optionally filtered.
        /// </summary>
        Task<List<RentalDisplayRecord>> GetAllRentalsForDisplayAsync(
            int pageNumber = 1,
            int pageSize = 25,
            string? searchTerm = null,
            DateTime? filterStartDate = null, // Optional date filters
            DateTime? filterEndDate = null);

        /// <summary>
        /// Gets the total count of rentals, optionally filtered.
        /// </summary>
        Task<int> GetTotalRentalCountAsync(
            string? searchTerm = null,
            DateTime? filterStartDate = null,
            DateTime? filterEndDate = null);

        /// <summary>
        /// Gets a specific rental by its ID (full RentalRecord for editing).
        /// </summary>
        Task<RentalRecord?> GetRentalByIdAsync(int rentalId); // For editing

        /// <summary>
        /// Updates an existing rental record.
        /// </summary>
        Task<bool> UpdateRentalAsync(RentalRecord rental); // For editing

        /// <summary>
        /// Deletes a rental record by its ID.
        /// </summary>
        /// <param name="rentalId"></param>
        /// <returns></returns>
        Task<bool> DeleteRentalAsync(int rentalId);
    }
}