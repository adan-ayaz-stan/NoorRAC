using System.Threading.Tasks;
using NoorRAC.Models;

namespace NoorRAC.Services
{
    /// <summary>
    /// Defines operations related to customer data management.
    /// </summary>
    public interface ICustomerService
    {
        /// <summary>
        /// Adds a new customer record to the database.
        /// </summary>
        /// <param name="customer">The customer data to add.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        Task<bool> AddCustomerAsync(CustomerRecord customer);

        /// <summary>
        /// Returns a paginated list of customers, optionally filtered by a search term.
        /// </summary>
        /// <param name="pageNumber">The current page number.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="searchTerm">Optional search term to filter customers.</param>
        /// <returns>A list of customer records.</returns>
        Task<List<CustomerRecord>> GetAllCustomersAsync(int pageNumber = 1, int pageSize = 25, string? searchTerm = null);

        /// <summary>
        /// Gets the total count of customers, optionally filtered by a search term.
        /// </summary>
        /// <param name="searchTerm">Optional search term to filter the count.</param>
        /// <returns>The total number of matching customers.</returns>
        Task<int> GetTotalCustomerCountAsync(string? searchTerm = null);

        // Future methods could include:
        // Task<CustomerRecord?> GetCustomerByIdAsync(int id);
        // Task<bool> UpdateCustomerAsync(CustomerRecord customer);
        // Task<bool> DeleteCustomerAsync(int id);

        /// <summary>
        /// Gets a specific customer by their ID.
        /// </summary>
        /// <param name="id">The ID of the customer to retrieve.</param>
        /// <returns>The customer record if found; otherwise, null.</returns>
        Task<CustomerRecord?> GetCustomerByIdAsync(int id); // New

        /// <summary>
        /// Updates an existing customer's information.
        /// </summary>
        /// <param name="customer">The customer data with updates.</param>
        /// <returns>True if the update was successful; otherwise, false.</returns>
        Task<bool> UpdateCustomerAsync(CustomerRecord customer); // New

        /// <summary>
        /// Deletes a customer by their ID.
        /// </summary>
        /// <param name="id">The ID of the customer to delete.</param>
        /// <returns>True if deletion was successful; otherwise, false.</returns>
        Task<bool> DeleteCustomerAsync(int id); // New
    }
}
