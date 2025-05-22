// NoorRAC/Models/RentalDisplayRecord.cs
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace NoorRAC.Models
{
    public partial class RentalDisplayRecord : ObservableObject
    {
        [ObservableProperty]
        private int _id; // Rental ID

        [ObservableProperty]
        private string? _clientName; // Fetched from customer table via customer_cnic

        [ObservableProperty]
        private string? _carType; // Combined "CarMake CarModel"

        [ObservableProperty]
        private string? _carRegistrationNumber; // This is the car_registration_no from rental table

        [ObservableProperty]
        private string? _status; // "Finished", "Ongoing", "Scheduled" (or "Upcoming")

        [ObservableProperty]
        private DateTime _startDate;

        [ObservableProperty]
        private DateTime _endDate;

        // For formatted display in DataGrid
        public string StartDateString => StartDate.ToString("yyyy-MM-dd");
        public string EndDateString => EndDate.ToString("yyyy-MM-dd");
    }
}