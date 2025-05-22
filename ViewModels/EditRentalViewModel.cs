// NoorRAC/ViewModels/EditRentalViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoorRAC.Models;
using NoorRAC.Services;
using NoorRAC.ValidationAttributes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace NoorRAC.ViewModels
{
    public partial class EditRentalViewModel : ObservableValidator
    {
        private readonly App.NavigationServiceFactory _navigationServiceFactory;
        private readonly ICustomerService _customerService;
        private readonly ICarService _carService;
        private readonly IRentalService _rentalService;
        private readonly IPaymentService _paymentService; // Still needed to load initial payment for summary

        private RentalRecord? _originalRental;
        private PaymentRecord? _initialPaymentForDisplay; // To store loaded initial payment for summary

        [ObservableProperty]
        private int _rentalId;

        // --- Rental Properties ---
        [ObservableProperty][NotifyDataErrorInfo][Required] private DateTime _startDate;
        [ObservableProperty][NotifyDataErrorInfo][Required][property: DateGreaterThan(nameof(StartDate))] private DateTime _endDate;
        [ObservableProperty] private string? _rentalArea;
        [ObservableProperty] private string? _otherInformation;
        [ObservableProperty][NotifyDataErrorInfo][Required] private CustomerRecord? _selectedCustomer;
        [ObservableProperty][NotifyDataErrorInfo][Required] private Car? _selectedCar;

        // --- PAYMENT SECTION REMOVED from editable fields ---
        // We'll only display existing payment info in the summary.

        // --- Summary Panel Properties ---
        [ObservableProperty] private decimal _summaryTotalAmount;
        [ObservableProperty] private decimal _summaryAmountPaidFromInitialPayment; // Renamed
        [ObservableProperty] private decimal _summaryAdvanceOrDue;
        [ObservableProperty] private string _summaryAdvanceOrDueLabel = "Total Due";

        [ObservableProperty] private ObservableCollection<CustomerRecord> _availableCustomers = new();
        [ObservableProperty] private ObservableCollection<Car> _availableCars = new();
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private bool _isLoadingCars;

        // Error Message Properties (only for rental fields now)
        public string? StartDateErrorMessage => GetFirstError(nameof(StartDate));
        public string? EndDateErrorMessage => GetFirstError(nameof(EndDate));
        public string? SelectedCustomerErrorMessage => GetFirstError(nameof(SelectedCustomer));
        public string? SelectedCarErrorMessage => GetFirstError(nameof(SelectedCar));
        private string? GetFirstError(string propertyName)
        {
            var errors = GetErrors(propertyName)?.OfType<ValidationResult>();
            return errors?.FirstOrDefault()?.ErrorMessage;
        }


        public string BreadcrumbRentalIdentifier => $"Rental ID {RentalId} :: {SelectedCar?.RegistrationNumber ?? _originalRental?.CarId.ToString() ?? "Edit"}";

        public EditRentalViewModel(
            App.NavigationServiceFactory navigationServiceFactory,
            ICustomerService customerService,
            ICarService carService,
            IRentalService rentalService,
            IPaymentService paymentService)
        {
            _navigationServiceFactory = navigationServiceFactory;
            _customerService = customerService;
            _carService = carService;
            _rentalService = rentalService;
            _paymentService = paymentService;
            ErrorsChanged += OnErrorsChanged;
        }

        private void OnErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(StartDate)) OnPropertyChanged(nameof(StartDateErrorMessage));
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(EndDate)) OnPropertyChanged(nameof(EndDateErrorMessage));
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(SelectedCustomer)) OnPropertyChanged(nameof(SelectedCustomerErrorMessage));
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(SelectedCar)) OnPropertyChanged(nameof(SelectedCarErrorMessage));
        }

        public async Task LoadRentalAsync(int rentalId)
        {
            RentalId = rentalId;
            IsLoading = true;
            try
            {
                var customersTask = _customerService.GetAllCustomersAsync(1, 200);
                _originalRental = await _rentalService.GetRentalByIdAsync(rentalId);

                if (_originalRental == null) { MessageBox.Show($"Rental ID {rentalId} not found.", "Error"); NavigateToRentals(); return; }

                // Load cars based on original rental's dates (or all cars and handle selection)
                var carsTask = _carService.GetAvailableCarsAsync(_originalRental.StartDate, _originalRental.EndDate);
                _initialPaymentForDisplay = await _paymentService.GetInitialPaymentByRentalIdAsync(rentalId);

                await Task.WhenAll(customersTask, carsTask);

                AvailableCustomers.Clear(); if (customersTask.Result != null) foreach (var c in customersTask.Result) AvailableCustomers.Add(c);
                AvailableCars.Clear(); if (carsTask.Result != null) foreach (var c in carsTask.Result) AvailableCars.Add(c);

                StartDate = _originalRental.StartDate;
                EndDate = _originalRental.EndDate;
                RentalArea = _originalRental.RentalArea;
                OtherInformation = _originalRental.OtherInformation;
                // TotalDue from original rental is used for initial summary if needed, but summary recalc is primary

                SelectedCustomer = AvailableCustomers.FirstOrDefault(c => c.ID == _originalRental.CustomerId);

                var currentRentalCarDetails = await _carService.GetCarByIdAsync(_originalRental.CarId);
                if (currentRentalCarDetails != null && !AvailableCars.Any(c => c.Id == currentRentalCarDetails.Id))
                {
                    AvailableCars.Add(currentRentalCarDetails);
                }
                SelectedCar = AvailableCars.FirstOrDefault(c => c.Id == _originalRental.CarId);


                UpdateSummaryPanel(); // This will use _initialPaymentForDisplay
                ClearErrors();
                ValidateAllProperties();
            }
            catch (Exception ex) { MessageBox.Show($"Error loading rental: {ex.Message}", "Load Error"); NavigateToRentals(); }
            finally { IsLoading = false; OnPropertyChanged(nameof(BreadcrumbRentalIdentifier)); }
        }

        private async Task LoadAvailableCarsForSelectedDatesAsync()
        {
            // ... (Logic to load available cars based on StartDate and EndDate,
            // ensuring the _originalRental's car is selectable, as before) ...
            if (StartDate.Date >= EndDate.Date) { AvailableCars.Clear(); SelectedCar = null; UpdateSummaryPanel(); return; }
            IsLoadingCars = true;
            Car? currentDisplaySelection = SelectedCar; // Store current UI selection

            try
            {
                var cars = await _carService.GetAvailableCarsAsync(StartDate, EndDate);
                AvailableCars.Clear();

                // Ensure the car originally assigned to this rental is in the list for selection,
                // as it's valid for THIS rental being edited, even if not for a NEW one.
                bool originalCarAdded = false;
                if (_originalRental != null && _originalRental.CarId != 0)
                {
                    var originalCarDetails = await _carService.GetCarByIdAsync(_originalRental.CarId);
                    if (originalCarDetails != null)
                    {
                        AvailableCars.Add(originalCarDetails);
                        originalCarAdded = true;
                    }
                }

                if (cars != null)
                {
                    foreach (var c in cars)
                    {
                        if (!AvailableCars.Any(ac => ac.Id == c.Id)) // Add if not already the original car
                        {
                            AvailableCars.Add(c);
                        }
                    }
                }

                // Try to maintain selection or re-select original
                if (currentDisplaySelection != null && AvailableCars.Any(c => c.Id == currentDisplaySelection.Id))
                {
                    SelectedCar = AvailableCars.First(c => c.Id == currentDisplaySelection.Id);
                }
                else if (originalCarAdded && _originalRental != null)
                {
                    SelectedCar = AvailableCars.FirstOrDefault(c => c.Id == _originalRental.CarId);
                }
                else
                {
                    SelectedCar = null; // Or first available if that's preferred
                }
            }
            catch (Exception ex) { MessageBox.Show($"Error loading available cars: {ex.Message}"); AvailableCars.Clear(); SelectedCar = null; }
            finally { IsLoadingCars = false; UpdateSummaryPanel(); ValidateProperty(SelectedCar, nameof(SelectedCar)); }
        }

        // Property Changed Handlers
        partial void OnStartDateChanged(DateTime value) { if (value.Date >= EndDate.Date) EndDate = value.Date.AddDays(1); UpdateSummaryPanel(); ValidateProperty(value, nameof(StartDate)); ValidateProperty(EndDate, nameof(EndDate)); _ = LoadAvailableCarsForSelectedDatesAsync(); }
        partial void OnEndDateChanged(DateTime value) { if (value.Date <= StartDate.Date) StartDate = value.Date.AddDays(-1); UpdateSummaryPanel(); ValidateProperty(value, nameof(EndDate)); ValidateProperty(StartDate, nameof(StartDate)); _ = LoadAvailableCarsForSelectedDatesAsync(); }
        partial void OnSelectedCarChanged(Car? value) => UpdateSummaryPanel();
        // No OnAmountPaidChanged needed as it's not an input field now

        private void UpdateSummaryPanel()
        {
            if (SelectedCar == null || StartDate.Date >= EndDate.Date)
            {
                SummaryTotalAmount = 0;
            }
            else
            {
                TimeSpan rentalDuration = EndDate.Date - StartDate.Date;
                int numberOfDays = (int)Math.Max(1, rentalDuration.TotalDays + 1);
                SummaryTotalAmount = numberOfDays * SelectedCar.RentPerDay;
            }

            // Display initial payment info if it exists
            SummaryAmountPaidFromInitialPayment = _initialPaymentForDisplay?.AmountPaid ?? 0;

            SummaryAdvanceOrDue = SummaryTotalAmount - SummaryAmountPaidFromInitialPayment;
            SummaryAdvanceOrDueLabel = "Remaining Due on Rental"; // Or "Paid in Full" if due is <=0

            OnPropertyChanged(nameof(SummaryTotalAmount));
            OnPropertyChanged(nameof(SummaryAmountPaidFromInitialPayment));
            OnPropertyChanged(nameof(SummaryAdvanceOrDue));
            OnPropertyChanged(nameof(SummaryAdvanceOrDueLabel));
        }

        [RelayCommand(CanExecute = nameof(CanSaveChanges))]
        private async Task UpdateRentalAsync()
        {
            ValidateAllProperties();
            if (HasErrors) { MessageBox.Show("Please correct validation errors.", "Validation Error"); return; }
            if (_originalRental == null || SelectedCustomer == null || SelectedCar == null) { MessageBox.Show("Rental data error.", "Error"); return; }
            UpdateSummaryPanel(); // Final calculation of TotalDue
            var updatedRental = new RentalRecord
            {
                ID = _originalRental.ID,
                CustomerId = SelectedCustomer.ID,
                CarId = SelectedCar.Id,
                StartDate = this.StartDate,
                EndDate = this.EndDate,
                RentalArea = this.RentalArea,
                OtherInformation = this.OtherInformation,
                TotalDue = SummaryTotalAmount // This is the new calculated total due
            };
            IsLoading = true;
            try
            {
                // Call the simplified UpdateRentalAsync that doesn't take payment
                bool success = await _rentalService.UpdateRentalAsync(updatedRental);
                if (success)
                {
                    MessageBox.Show($"Rental ID {updatedRental.ID} updated successfully!", "Success");
                    NavigateToRentals();
                }
                else { MessageBox.Show("Failed to update rental.", "Update Error"); }
            }
            catch (InvalidOperationException ex)
            {
                // Handle the specific conflict exception
                MessageBox.Show(ex.Message, "Rental Conflict");
            }
            catch (Exception ex) { MessageBox.Show($"Error updating rental: {ex.Message}", "Error"); }
            finally { IsLoading = false; }
        }
        private bool CanSaveChanges() => _originalRental != null && !IsLoading;

        [RelayCommand(CanExecute = nameof(CanSaveChanges))]
        private async Task DeleteRentalAsync()
        {
            // ... (Delete logic as before, calling _rentalService.DeleteRentalAsync)
            if (_originalRental == null) return;
            if (MessageBox.Show($"Delete Rental ID {_originalRental.ID}?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                IsLoading = true;
                try { bool s = await _rentalService.DeleteRentalAsync(_originalRental.ID); if (s) NavigateToRentals(); else MessageBox.Show("Failed to delete."); }
                catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
                finally { IsLoading = false; }
            }
        }

        [RelayCommand] private void CloseForm() => NavigateToRentals();

        // Sidebar Nav
        [RelayCommand] private void NavigateToDashboard() => _navigationServiceFactory.Create<DashboardViewModel>().Navigate();
        [RelayCommand] private void NavigateToRentals() => _navigationServiceFactory.Create<RentalsViewModel>().Navigate();
        [RelayCommand] private void NavigateToCustomers() => _navigationServiceFactory.Create<CustomersViewModel>().Navigate();
        [RelayCommand] private void NavigateToPayments() => _navigationServiceFactory.Create<PaymentsViewModel>().Navigate();
        [RelayCommand] private void NavigateToExpenses() => _navigationServiceFactory.Create<ExpensesViewModel>().Navigate();
        [RelayCommand] private void NavigateToFinances() => _navigationServiceFactory.Create<FinancesViewModel>().Navigate();
        [RelayCommand] private void NavigateToCars() => _navigationServiceFactory.Create<CarsViewModel>().Navigate();
        [RelayCommand] private void Logout() => _navigationServiceFactory.Create<LoginViewModel>().Navigate();
    }
}