// NoorRAC/Models/RentalRecord.cs
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace NoorRAC.Models
{
    public partial class RentalRecord : ObservableObject
    {
        [ObservableProperty]
        private int _iD;

        [ObservableProperty]
        private int _carId; // Foreign Key to Car.Id

        [ObservableProperty]
        private int _customerId; // Foreign Key to Customer.Id

        [ObservableProperty]
        private DateTime _startDate = DateTime.Today;

        [ObservableProperty]
        private DateTime _endDate = DateTime.Today.AddDays(1);

        [ObservableProperty]
        private string? _rentalArea;

        [ObservableProperty]
        private string? _otherInformation; // Mapped to 'comments' in DB

        [ObservableProperty]
        private decimal _totalDue; // Should be decimal for currency

        // Not directly in DB 'rental' table, but part of the UI flow / related data
        [ObservableProperty]
        private string? _status; // e.g., "Booked", "Active", "Completed" - This would be calculated or set by logic
    }
}