// NoorRAC/Models/DailyFinancialSummary.cs
using System;

namespace NoorRAC.Models
{
    public class DailyFinancialSummary
    {
        public DateTime Date { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal TotalExpenses { get; set; }
    }
}