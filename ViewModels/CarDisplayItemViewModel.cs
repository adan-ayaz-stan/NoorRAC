// NoorRAC/ViewModels/CarDisplayItemViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoorRAC.Models; // Your Car model
using System;
using System.Windows.Media; // For Brush

namespace NoorRAC.ViewModels
{
    public partial class CarDisplayItemViewModel : ObservableObject
    {
        private readonly Car _car;
        private readonly Action<Car> _onEditCar; // Action to call when EditCar is clicked
        private readonly Action<Car> _onSeeMore;   // Action to call when SeeMore is clicked

        public Car CarData => _car; // Expose the underlying Car model if needed

        public CarDisplayItemViewModel(Car car, Action<Car> onEditCar, Action<Car> onSeeMore)
        {
            _car = car;
            _onEditCar = onEditCar;
            _onSeeMore = onSeeMore;

            // Initialize placeholder or calculated properties
            // These would be updated based on actual rental data for this car
            Status = "Available"; // Placeholder
            StatusBackground = Brushes.LightGreen; // Placeholder
            StatusForeground = Brushes.DarkGreen; // Placeholder
            TotalRentalsCount = 0; // Placeholder
            IncomeThisMonth = "Rs. 0"; // Placeholder
            RentalDaysThisMonth = $"00/{DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)}"; // Placeholder
            EarliestAvailableDate = $"Available Now"; // Placeholder (or calculate actual date)
        }

        public string LicensePlate => _car.RegistrationNumber;
        public string ModelInfo => $"{_car.CarMake} {_car.CarModel}";
        public string RentPerDay => $"Rs. {_car.RentPerDay:N0}"; // N0 for no decimals

        // Properties for dynamic display (these would be calculated based on rental data)
        [ObservableProperty]
        private string _status = string.Empty;

        [ObservableProperty]
        private Brush _statusBackground = Brushes.Transparent;

        [ObservableProperty]
        private Brush _statusForeground = Brushes.Black;

        [ObservableProperty]
        private int _totalRentalsCount; // Placeholder: Total times this car has been rented

        [ObservableProperty]
        private string _incomeThisMonth = string.Empty; // Placeholder: e.g., "Rs. 15,000"

        [ObservableProperty]
        private string _rentalDaysThisMonth = string.Empty; // Placeholder: e.g., "10/30"

        [ObservableProperty]
        private string _earliestAvailableDate = string.Empty; // Placeholder: e.g., "15-Jul-2024" or "Available Now"


        // --- Commands for the card's buttons ---
        [RelayCommand]
        private void EditCar()
        {
            _onEditCar?.Invoke(_car);
        }

        [RelayCommand]
        private void SeeMore()
        {
            _onSeeMore?.Invoke(_car);
        }

        // Call this method when actual rental data for this car is fetched and processed
        public void UpdateRentalStatus(bool isRented, DateTime? nextAvailableDate, int totalRentals, decimal incomeThisMonth, int rentalDaysThisMonth)
        {
            TotalRentalsCount = totalRentals;
            IncomeThisMonth = $"Rs. {incomeThisMonth:N0}";
            int daysInMonth = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
            RentalDaysThisMonth = $"{rentalDaysThisMonth:D2}/{daysInMonth}";

            if (isRented)
            {
                Status = "Rented";
                StatusBackground = Brushes.LightCoral;
                StatusForeground = Brushes.DarkRed;
                EarliestAvailableDate = nextAvailableDate.HasValue ? nextAvailableDate.Value.ToString("dd-MMM-yyyy") : "Rented Indefinitely";
            }
            else
            {
                Status = "Available";
                StatusBackground = Brushes.LightGreen;
                StatusForeground = Brushes.DarkGreen;
                EarliestAvailableDate = "Available Now";
            }
        }
    }
}