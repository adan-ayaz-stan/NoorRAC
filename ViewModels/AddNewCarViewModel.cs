// NoorRAC/ViewModels/AddNewCarViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoorRAC.Models;
using NoorRAC.Services;
using Org.BouncyCastle.Asn1.X509;
using System;
using System.ComponentModel.DataAnnotations; // For validation attributes
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace NoorRAC.ViewModels
{
    public partial class AddNewCarViewModel : ObservableValidator // Use ObservableValidator for INotifyDataErrorInfo
    {
        private readonly App.NavigationServiceFactory _navigationServiceFactory;
        private readonly ICarService _carService; // Inject ICarService

        // --- Form Properties ---
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Registration number is required.")]
        [RegularExpression(@"^[A-Z]{1,4}-?\d{1,5}$", ErrorMessage = "Format: A-1 to WXYZ-12345")] // Example regex, adjust
        private string? _registrationNumber;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Car model is required.")]
        private string? _carModel;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Car make is required.")]
        private string? _carMake;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(100, 100000, ErrorMessage = "Rent must be between 100 and 100,000.")] // Adjust range as needed
        private int _rentPerDay;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Owner name is required.")]
        private string? _ownerName;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "CNIC is required")]

        [RegularExpression(@"^\d{5}-\d{7}-\d{1}$", ErrorMessage = "CNIC must be in the format 12345-1234567-1.")]
        private string? _ownerCNIC;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(0, 1000000, ErrorMessage = "Lending fees must be a positive value.")] // Adjust range
        private int _ownerLendingFees;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Owner phone number is required.")]
        [RegularExpression(@"^\d{4}-\d{7}$", ErrorMessage = "Phone number must be in the format 0300-0000000.")]
        private string _ownerPhone;

        [ObservableProperty]
        private bool _isLoading;

        // --- Error Message Properties (for direct TextBlock binding if not using Validation.ErrorTemplate) ---
        public string? RegistrationNumberErrorMessage =>
    (GetErrors(nameof(RegistrationNumber)).FirstOrDefault() as ValidationResult)?.ErrorMessage;

        public string? CarModelErrorMessage =>
    (GetErrors(nameof(CarModel)).FirstOrDefault() as ValidationResult)?.ErrorMessage;
        public string? CarMakeErrorMessage =>
            (GetErrors(nameof(CarMake)).FirstOrDefault() as ValidationResult)?.ErrorMessage;
        public string? RentPerDayErrorMessage =>
            (GetErrors(nameof(RentPerDay)).FirstOrDefault() as ValidationResult)?.ErrorMessage;
        public string? OwnerNameErrorMessage =>
            (GetErrors(nameof(OwnerName)).FirstOrDefault() as ValidationResult)?.ErrorMessage;
        public string? OwnerCNICErrorMessage =>
            (GetErrors(nameof(OwnerCNIC)).FirstOrDefault() as ValidationResult)?.ErrorMessage;
        public string? OwnerLendingFeesErrorMessage =>
            (GetErrors(nameof(OwnerLendingFees)).FirstOrDefault() as ValidationResult)?.ErrorMessage;
        public string? OwnerPhoneErrorMessage =>
            (GetErrors(nameof(OwnerPhone)).FirstOrDefault() as ValidationResult)?.ErrorMessage;



        public AddNewCarViewModel(App.NavigationServiceFactory navigationServiceFactory, ICarService carService)
        {
            _navigationServiceFactory = navigationServiceFactory;
            _carService = carService; // Store injected service

            // Subscribe to ErrorsChanged to update custom error message properties
            ErrorsChanged += (s, e) => UpdateErrorMessages();
        }

        private void UpdateErrorMessages()
        {
            OnPropertyChanged(nameof(RegistrationNumberErrorMessage));
            OnPropertyChanged(nameof(CarModelErrorMessage));
            OnPropertyChanged(nameof(CarMakeErrorMessage));
            OnPropertyChanged(nameof(RentPerDayErrorMessage));
            OnPropertyChanged(nameof(OwnerNameErrorMessage));
            OnPropertyChanged(nameof(OwnerCNICErrorMessage));
            OnPropertyChanged(nameof(OwnerLendingFeesErrorMessage));
            OnPropertyChanged(nameof(OwnerPhoneErrorMessage));
        }

        [RelayCommand]
        private async Task SaveCarAsync() // Renamed for clarity and async nature
        {
            ClearErrors();
            ValidateAllProperties();

            if (HasErrors)
            {
                MessageBox.Show("Please correct the validation errors.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var newCar = new Car
            {
                RegistrationNumber = this.RegistrationNumber!, // Not null due to validation
                CarModel = this.CarModel!,
                CarMake = this.CarMake!,
                RentPerDay = this.RentPerDay,
                OwnerName = this.OwnerName!,
                OwnerCNIC = this.OwnerCNIC!,
                OwnerLendingFees = this.OwnerLendingFees,
                OwnerPhone = this.OwnerPhone!
            };

            IsLoading = true;
            try
            {
                bool success = await _carService.AddCarAsync(newCar);
                if (success)
                {
                    MessageBox.Show($"Car '{newCar.CarMake} {newCar.CarModel} ({newCar.RegistrationNumber})' saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    // Navigate to Cars list view
                    _navigationServiceFactory.Create<CarsViewModel>().Navigate();
                }
                else
                {
                    MessageBox.Show("Failed to save car. It might already exist or there was a database error.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // Log ex.ToString()
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void CloseForm() // For the "Close Payment" button, assuming it means cancel/close this form
        {
            _navigationServiceFactory.Create<CarsViewModel>().Navigate(); // Navigate back to the main Cars view
        }

        // --- Sidebar Navigation Commands ---
        [RelayCommand] private void NavigateToDashboard() => _navigationServiceFactory.Create<DashboardViewModel>().Navigate();
        [RelayCommand] private void NavigateToRentals() => _navigationServiceFactory.Create<RentalsViewModel>().Navigate();
        [RelayCommand] private void NavigateToCustomers() => _navigationServiceFactory.Create<CustomersViewModel>().Navigate();
        [RelayCommand] private void NavigateToPayments() => _navigationServiceFactory.Create<PaymentsViewModel>().Navigate();
        [RelayCommand] private void NavigateToExpenses() => _navigationServiceFactory.Create<ExpensesViewModel>().Navigate();
        [RelayCommand] private void NavigateToFinances() => _navigationServiceFactory.Create<FinancesViewModel>().Navigate();
        [RelayCommand] private void NavigateToCars() => _navigationServiceFactory.Create<CarsViewModel>().Navigate(); // Navigate to main cars list
        [RelayCommand] private void Logout() => _navigationServiceFactory.Create<LoginViewModel>().Navigate();
    }
}