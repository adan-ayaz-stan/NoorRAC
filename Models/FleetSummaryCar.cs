// NoorRAC/Models/FleetSummaryCar.cs
using System;

namespace NoorRAC.Models
{
    public class FleetSummaryCar
    {
        public int CarId { get; set; }
        public string RegistrationNumber { get; set; } = "N/A";
        public string ArrivingStatus { get; set; } = "Available"; // e.g., "Today", "In 3 Days", "Available"
        public string NextRentalStatus { get; set; } = "Available"; // e.g., "16-05-25", "Available", "Booked"
        public bool IsCurrentlyRented { get; set; }
        public DateTime? CurrentRentalEndDate { get; set; }
        public DateTime? NextBookingStartDate { get; set; }
    }
}