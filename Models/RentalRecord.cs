// NoorRAC/Models/RentalRecord.cs
using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NoorRAC.Models
{
    public partial class RentalRecord : ObservableObject
    {
        [ObservableProperty]
        private int _iD;

        [ObservableProperty]
        private string? _name; // Client Name

        [ObservableProperty]
        private string? _carType;

        [ObservableProperty]
        private string? _carNumber;

        [ObservableProperty]
        private int? _rentPerDay;

        [ObservableProperty]
        private string? _status;

        [ObservableProperty]
        private DateTime _startDate;

        [ObservableProperty]
        private DateTime _endDate;

        // Example of how you might want to format dates for display
        public string StartDateString => StartDate.ToString("yyyy-MM-dd");
        public string EndDateString => EndDate.ToString("yyyy-MM-dd");
    }
}