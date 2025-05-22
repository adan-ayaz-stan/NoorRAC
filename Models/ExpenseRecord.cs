// NoorRAC/Models/ExpenseRecord.cs
using System;

namespace NoorRAC.Models
{
    public class ExpenseRecord
    {
        public int Id { get; set; }
        public DateTime Date { get; set; } = DateTime.Today; // Default to today
        public int Amount { get; set; } // DB is INT
        public string? Description { get; set; }
        public int? CarId { get; set; } // Nullable Foreign Key to Car
    }
}