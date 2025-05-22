// NoorRAC/Models/FinancialOverviewStats.cs
namespace NoorRAC.Models
{
    public class FinancialOverviewStats
    {
        public decimal TotalIncomeForPeriod { get; set; } // Sum of all payments in period
        public decimal TotalExpensesForPeriod { get; set; } // Sum of all expenses in period
        public decimal NetProfitForPeriod => TotalIncomeForPeriod - TotalExpensesForPeriod;
        // You can add more stats like total due if needed from Rentals, etc.
        // For the XAML, "Received Payments" seems to be TotalIncomeForPeriod.
        // "Due Payments" is not directly from payments/expenses, it would come from rentals.
        // I'll assume "Income" in the XAML refers to TotalIncomeForPeriod.
        // And "Due Payments" will be a placeholder or you'll need to fetch it separately.
    }
}