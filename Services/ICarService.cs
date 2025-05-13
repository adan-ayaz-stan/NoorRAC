using NoorRAC.ViewModels; // Or NoorRAC.Models if you placed VMs there
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NoorRAC.Services
{
    public interface ICarService
    {
        /// <summary>
        /// Gets the count of cars currently available for rent.
        /// </summary>
        Task<int> GetAvailableCountAsync();

        /// <summary>
        /// Gets a summary view of recently available cars.
        /// </summary>
        /// <param name="count">The maximum number of cars to return.</param>
        Task<IEnumerable<CarSummaryViewModel>> GetRecentAvailableSummaryAsync(int count);

        // Add other Car related methods here (GetAll, GetById, Add, Update, Delete etc.)
    }
}