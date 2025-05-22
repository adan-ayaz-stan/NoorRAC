// NoorRAC/Models/FinancialTransactionRecord.cs
using System;

namespace NoorRAC.Models
{
    public enum TransactionType { Income, Expense }

    public class FinancialTransactionRecord
    {
        public int OriginalId { get; set; } // ID from either payment or expense table
        public string? Name { get; set; } // Customer Name for Income, Description for Expense
        public decimal Amount { get; set; } // Amount (use decimal for currency)
        public TransactionType Type { get; set; }
        public DateTime Date { get; set; }
        public string? Details { get; set; } // e.g., Payment Method, Car associated with expense
        public string SourceTable { get; set; } // "Payment" or "Expense" to know where it came from
    }
}