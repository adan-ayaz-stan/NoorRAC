// NoorRAC/Models/ExpenseDisplayRecord.cs
using System;

namespace NoorRAC.Models
{
    public class ExpenseDisplayRecord
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int Amount { get; set; } // Consistent with ExpenseRecord and DB
        public string? Description { get; set; }
        public string? CarInfo { get; set; } // e.g., "Car Reg: XYZ-123 (Make Model)" or "N/A"
        public int? CarId { get; set; } // Keep original CarId if needed for edit/delete logic
    }
}