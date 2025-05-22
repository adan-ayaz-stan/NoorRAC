// NoorRAC/Services/IPaymentService.cs
using NoorRAC.Models;
using System; // For DateTime
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NoorRAC.Services
{
    public interface IPaymentService
    {
        Task<bool> AddPaymentAsync(PaymentRecord payment);

        /// <summary>
        /// Gets a paginated list of payments, optionally filtered by date range and search term.
        /// </summary>
        Task<List<PaymentDisplayRecord>> GetAllPaymentsAsync(
            int pageNumber = 1,
            int pageSize = 25,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? searchTerm = null, string? overviewPeriodFilter = null); // Search term for customer name, rental info etc.

        /// <summary>
        /// Get payment by Id.
        /// </summary>
        /// <param name="paymentId"></param>
        /// <returns></returns>
        Task<PaymentRecord?> GetPaymentByIdAsync(int paymentId);

        /// <summary>
        /// Gets the total count of payments, optionally filtered.
        /// </summary>
        Task<int> GetTotalPaymentCountAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? searchTerm = null, string? overviewPeriodFilter = null);

        /// <summary>
        /// Gets payment overview statistics for a given period.
        /// </summary>
        Task<PaymentOverviewStats?> GetPaymentOverviewStatsAsync(DateTime filterFromDate, DateTime filterToDate); // e.g., "Today", "This Week"

        /// <summary>
        /// Gets a specific payment by its ID (full PaymentRecord for editing).
        /// </summary>
        /// <param name="rentalId"></param>
        /// <returns></returns>
        Task<PaymentRecord?> GetInitialPaymentByRentalIdAsync(int rentalId);

        /// <summary>
        /// Updates an existing payment record.
        /// </summary>
        /// <param name="payment"></param>
        /// <returns></returns>
        Task<bool> UpdatePaymentAsync(PaymentRecord payment); // If we update payment

        /// <summary>
        /// Gets the total due for a customer based on their CNIC.
        /// </summary>
        /// <param name="cnic"></param>
        /// <returns></returns>
        Task<int> GetTotalDueForCustomer(string cnic);

        /// <summary>
        /// Gets the remaining due for a customer based on their CNIC.
        /// </summary>
        /// <param name="cnic"></param>
        /// <returns></returns>
        Task<int> GetRemainingDueForCustomer(string cnic);

        /// <summary>
        /// Gets the remaining due for a rental based on its ID and customer ID.
        /// </summary>
        /// <param name="rentalId"></param>
        /// <param name="customerId"></param>
        /// <returns></returns>
        Task<int> GetRemainingDueForRental(int rentalId, int customerId);

    }

    // Helper class for stats
    public class PaymentOverviewStats
    {
        public decimal TotalPayments { get; set; }
        public decimal TotalReceivedPayments { get; set; } // Sum of payments where payment_date is before or on rental start_date
        public decimal TotalOutstandingDueOnRentals { get; set; } // Sum of (rental.total_due - sum of payments for that rental) for active/past rentals
        public decimal PercentageChangeFromLastPeriod { get; set; } // This remains complex to calculate accurately without historical snapshots
    }
}