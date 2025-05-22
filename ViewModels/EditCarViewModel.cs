// NoorRAC/ViewModels/EditCarViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoorRAC.Models;
using NoorRAC.Services;
using Org.BouncyCastle.Asn1.X509;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace NoorRAC.ViewModels
{
    public partial class EditCarViewModel : ObservableValidator
    {
        private readonly App.NavigationServiceFactory _navigationServiceFactory;
        private readonly ICarService _carService;

        private Car? _originalCar; // To store the initially loaded car data

        [ObservableProperty]
        private int _carId; // Set when loading

        // --- Editable Properties ---
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Registration number is required.")]
        [RegularExpression(@"^[A-Z]{2,3}-?\d{1,4}$", ErrorMessage = "Format: ABC-1234 or AB-123")]
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
        [Range(100, 100000, ErrorMessage = "Rent must be between 100 and 100,000.")]
        private int _rentPerDay;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Owner name is required.")]
        private string? _ownerName;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Owner CNIC is required.")]
        [RegularExpression(@"^\d{5}-\d{7}-\d{1}$", ErrorMessage = "CNIC must be in XXXXX-XXXXXXX-X format.")]
        private string? _ownerCNIC;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(0, 1000000, ErrorMessage = "Lending fees must be a positive value.")]
        private int _ownerLendingFees;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Owner phone number is required.")]
        [RegularExpression(@"^\d{4}-\d{7}$", ErrorMessage = "Phone number must be in the format 0300-0000000.")]
        private string _ownerPhone;

        [ObservableProperty]
        private bool _isLoading;

        // Error Message Properties
        public string? RegistrationNumberErrorMessage =>
    GetErrors(nameof(RegistrationNumber)).OfType<ValidationResult>().FirstOrDefault()?.ErrorMessage;

        public string? CarModelErrorMessage =>
            GetErrors(nameof(CarModel)).OfType<ValidationResult>().FirstOrDefault()?.ErrorMessage;

        public string? CarMakeErrorMessage =>
            GetErrors(nameof(CarMake)).OfType<ValidationResult>().FirstOrDefault()?.ErrorMessage;

        public string? RentPerDayErrorMessage =>
            GetErrors(nameof(RentPerDay)).OfType<ValidationResult>().FirstOrDefault()?.ErrorMessage;

        public string? OwnerNameErrorMessage =>
            GetErrors(nameof(OwnerName)).OfType<ValidationResult>().FirstOrDefault()?.ErrorMessage;

        public string? OwnerCNICErrorMessage =>
            GetErrors(nameof(OwnerCNIC)).OfType<ValidationResult>().FirstOrDefault()?.ErrorMessage;

        public string? OwnerLendingFeesErrorMessage =>
            GetErrors(nameof(OwnerLendingFees)).OfType<ValidationResult>().FirstOrDefault()?.ErrorMessage;

        public string? OwnerPhoneErrorMessage =>
            GetErrors(nameof(OwnerPhone)).OfType<ValidationResult>().FirstOrDefault()?.ErrorMessage;


        public string BreadcrumbCarIdentifier => _originalCar?.RegistrationNumber ?? "Edit Car";


        public EditCarViewModel(App.NavigationServiceFactory navigationServiceFactory, ICarService carService)
        {
            _navigationServiceFactory = navigationServiceFactory;
            _carService = carService;
            ErrorsChanged += (s, e) => UpdateErrorMessages();
        }

        private void UpdateErrorMessages()
        {
            OnPropertyChanged(nameof(RegistrationNumberErrorMessage));
            OnPropertyChanged(nameof(CarModelErrorMessage));
            OnPropertyChanged(nameof(OwnerCNICErrorMessage));
            OnPropertyChanged(nameof(OwnerLendingFeesErrorMessage));
            OnPropertyChanged(nameof(RentPerDayErrorMessage));
            OnPropertyChanged(nameof(OwnerNameErrorMessage));
            OnPropertyChanged(nameof(OwnerPhoneErrorMessage));
        }

        public async Task LoadCarAsync(int carId)
        {
            CarId = carId;
            IsLoading = true;
            try
            {
                _originalCar = await _carService.GetCarByIdAsync(carId);
                if (_originalCar != null)
                {
                    RegistrationNumber = _originalCar.RegistrationNumber;
                    CarModel = _originalCar.CarModel;
                    CarMake = _originalCar.CarMake;
                    RentPerDay = _originalCar.RentPerDay;
                    OwnerName = _originalCar.OwnerName;
                    OwnerCNIC = _originalCar.OwnerCNIC;
                    OwnerLendingFees = _originalCar.OwnerLendingFees;
                    OwnerPhone = _originalCar.OwnerPhone;

                    ClearErrors(); // Clear previous validation state
                    ValidateAllProperties(); // Validate loaded properties
                    OnPropertyChanged(nameof(BreadcrumbCarIdentifier)); // Update breadcrumb
                }
                else
                {
                    MessageBox.Show($"Car with ID {carId} not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    NavigateToCars(); // Go back to the main cars list
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading car details: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                NavigateToCars();
            }
            finally { IsLoading = false; }
        }

        [RelayCommand(CanExecute = nameof(CanSaveOrDelete))]
        private async Task UpdateCarAsync() // Renamed from SaveCar
        {
            ClearErrors();
            ValidateAllProperties();
            if (HasErrors)
            {
                MessageBox.Show("Please correct the validation errors.", "Validation Error");
                return;
            }

            if (_originalCar == null) return; // Should not happen if CanSave is true

            var carToUpdate = new Car
            {
                Id = _originalCar.Id, // Keep original ID
                RegistrationNumber = this.RegistrationNumber!,
                CarModel = this.CarModel!,
                CarMake = this.CarMake!,
                RentPerDay = this.RentPerDay,
                OwnerName = this.OwnerName!,
                OwnerCNIC = this.OwnerCNIC,
                OwnerLendingFees = this.OwnerLendingFees,
                OwnerPhone = this.OwnerPhone!
            };

            IsLoading = true;
            try
            {
                bool success = await _carService.UpdateCarAsync(carToUpdate);
                if (success)
                {
                    MessageBox.Show("Car details updated successfully!", "Success");
                    NavigateToCars();
                }
                else { MessageBox.Show("Failed to update car details.", "Update Error"); }
            }
            catch (Exception ex) { MessageBox.Show($"Error updating car: {ex.Message}", "Error"); }
            finally { IsLoading = false; }
        }

        [RelayCommand(CanExecute = nameof(CanSaveOrDelete))]
        private async Task DeleteCarAsync()
        {
            if (_originalCar == null) return;

            if (MessageBox.Show($"Are you sure you want to delete car '{_originalCar.RegistrationNumber} - {_originalCar.CarMake} {_originalCar.CarModel}'? This action cannot be undone.",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                IsLoading = true;
                try
                {
                    bool success = await _carService.DeleteCarAsync(_originalCar.Id);
                    if (success)
                    {
                        MessageBox.Show("Car deleted successfully.", "Success");
                        NavigateToCars();
                    }
                    else { MessageBox.Show("Failed to delete car. It might be in use.", "Delete Error"); }
                }
                catch (Exception ex) { MessageBox.Show($"Error deleting car: {ex.Message}", "Error"); }
                finally { IsLoading = false; }
            }
        }

        private bool CanSaveOrDelete() => _originalCar != null && !IsLoading;


        [RelayCommand]
        private void CloseForm() => NavigateToCars();

        // --- Sidebar Navigation Commands ---
        [RelayCommand] private void NavigateToDashboard() => _navigationServiceFactory.Create<DashboardViewModel>().Navigate();
        [RelayCommand] private void NavigateToRentals() => _navigationServiceFactory.Create<RentalsViewModel>().Navigate();
        [RelayCommand] private void NavigateToCustomers() => _navigationServiceFactory.Create<CustomersViewModel>().Navigate();
        [RelayCommand] private void NavigateToPayments() => _navigationServiceFactory.Create<PaymentsViewModel>().Navigate();
        [RelayCommand] private void NavigateToExpenses() => _navigationServiceFactory.Create<ExpensesViewModel>().Navigate();
        [RelayCommand] private void NavigateToFinances() => _navigationServiceFactory.Create<FinancesViewModel>().Navigate();
        [RelayCommand] private void NavigateToCars() => _navigationServiceFactory.Create<CarsViewModel>().Navigate(); // Changed from NavigateToCars
        [RelayCommand] private void Logout() => _navigationServiceFactory.Create<LoginViewModel>().Navigate();
    }
}