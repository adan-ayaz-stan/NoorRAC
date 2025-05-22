// NoorRAC/Services/IExpenseService.cs
using NoorRAC.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NoorRAC.Services
{
    public interface IExpenseService
    {
        Task<bool> AddExpenseAsync(ExpenseRecord expense);
        Task<bool> UpdateExpenseAsync(ExpenseRecord expense);
        Task<bool> DeleteExpenseAsync(int expenseId);
        Task<ExpenseRecord?> GetExpenseByIdAsync(int expenseId);
        Task<List<ExpenseDisplayRecord>> GetAllExpensesAsync(
            int pageNumber = 1,
            int pageSize = 25,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? searchTerm = null);
        Task<int> GetTotalExpensesCountAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? searchTerm = null);
    }
}