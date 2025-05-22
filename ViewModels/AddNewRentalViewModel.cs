// NoorRAC/ViewModels/AddNewRentalViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoorRAC.Models;
using NoorRAC.Services;
using NoorRAC.ValidationAttributes; // For DateGreaterThanAttribute
using System;
using System.Collections.Generic;    // For List<ValidationResult>
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows; // For Visibility and MessageBox

namespace NoorRAC.ViewModels
{
    public partial class AddNewRentalViewModel : ObservableValidator
    {
        private readonly App.NavigationServiceFactory _navigationServiceFactory;
        private readonly ICustomerService _customerService;
        private readonly ICarService _carService;
        private readonly IRentalService _rentalService;

        // --- Rental Properties ---
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Start date is required.")]
        private DateTime _startDate = DateTime.Today;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "End date is required.")]
        [property: DateGreaterThan(nameof(StartDate), ErrorMessage = "End date must be after Start date.")] // Apply to generated property
        private DateTime _endDate = DateTime.Today.AddDays(1);

        [ObservableProperty]
        private string? _rentalArea;

        [ObservableProperty]
        private string? _otherInformation; //

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Please select a customer.")]
        private CustomerRecord? _selectedCustomer;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Please select a car.")]
        private Car? _selectedCar;

        // --- Payment Properties ---
        [ObservableProperty]
        private bool _customerWillPayNow; // For the CheckBox

        [ObservableProperty]
        //[NotifyDataErrorInfo] // This is for the INotifyDataErrorInfo mechanism
        [property: CustomValidation(typeof(AddNewRentalViewModel), nameof(ValidatePaymentAmount))] // Applied to the generated public property 'AmountPaid'
        private decimal _amountPaid;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [CustomValidation(typeof(AddNewRentalViewModel), nameof(ValidatePaymentMethod))]
        private string? _selectedPaymentMethod;
        public ObservableCollection<string> PaymentMethods { get; } = new() { "Cash", "Card", "Online", "Other" };

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Payment date is required.")] // Only truly required if paying now, but often good to have a date.
                                                               // CustomValidation could also handle this if needed.
        private DateTime _paymentDate = DateTime.Today;

        // --- Summary Panel Properties ---
        [ObservableProperty]
        private decimal _summaryTotalAmount;
        [ObservableProperty]
        private decimal _summaryAmountPaid;
        [ObservableProperty]
        private decimal _summaryAdvanceOrDue;
        [ObservableProperty]
        private string _summaryAdvanceOrDueLabel = "Total Due";

        // --- ComboBox Collections ---
        [ObservableProperty]
        private ObservableCollection<CustomerRecord> _availableCustomers = new();
        [ObservableProperty]
        private ObservableCollection<Car> _availableCars = new();

        [ObservableProperty]
        private bool _isLoading;
        [ObservableProperty] private bool _isLoadingCars;

        // Visibility for Payment Section
        public Visibility PaymentSectionVisibility => CustomerWillPayNow ? Visibility.Visible : Visibility.Collapsed;
        public Visibility SummaryPaidNowVisibility => CustomerWillPayNow ? Visibility.Visible : Visibility.Collapsed;


        // Error Message Properties for XAML binding
        public string? StartDateErrorMessage => GetFirstError(nameof(StartDate));
        public string? EndDateErrorMessage => GetFirstError(nameof(EndDate));
        public string? SelectedCustomerErrorMessage => GetFirstError(nameof(SelectedCustomer));
        public string? SelectedCarErrorMessage => GetFirstError(nameof(SelectedCar));
        public string? AmountPaidErrorMessage => GetFirstError(nameof(AmountPaid));
        public string? SelectedPaymentMethodErrorMessage => GetFirstError(nameof(SelectedPaymentMethod));
        public string? PaymentDateErrorMessage => GetFirstError(nameof(PaymentDate));

        private string? GetFirstError(string propertyName)
        {
            var errors = GetErrors(propertyName);
            if (errors != null)
            {
                var firstError = errors.FirstOrDefault();
                if (firstError is ValidationResult validationResult)
                {
                    return validationResult.ErrorMessage;
                }
                return firstError?.ToString(); // Fallback if it's not ValidationResult (should be)
            }
            return null;
        }

        public AddNewRentalViewModel(
            App.NavigationServiceFactory navigationServiceFactory,
            ICustomerService customerService,
            ICarService carService,
            IRentalService rentalService)
        {
            _navigationServiceFactory = navigationServiceFactory;
            _customerService = customerService;
            _carService = carService;
            _rentalService = rentalService;

            SelectedPaymentMethod = PaymentMethods.FirstOrDefault();

            ErrorsChanged += OnErrorsChanged; // Subscribe to ErrorsChanged
            _ = LoadInitialDropDownDataAsync();
            UpdateSummaryPanel(); // Initial update
        }

        private void OnErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
        {
            // Update specific error message properties or all if it's a general change
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(StartDate)) OnPropertyChanged(nameof(StartDateErrorMessage));
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(EndDate)) OnPropertyChanged(nameof(EndDateErrorMessage));
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(SelectedCustomer)) OnPropertyChanged(nameof(SelectedCustomerErrorMessage));
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(SelectedCar)) OnPropertyChanged(nameof(SelectedCarErrorMessage));
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(AmountPaid)) OnPropertyChanged(nameof(AmountPaidErrorMessage));
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(SelectedPaymentMethod)) OnPropertyChanged(nameof(SelectedPaymentMethodErrorMessage));
            
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(PaymentDate)) OnPropertyChanged(nameof(PaymentDateErrorMessage));
        }

        private async Task LoadInitialDropDownDataAsync()
        {
            IsLoading = true; // General loading for customers
            try
            {
                // Load customers (only once typically)
                var customers = await _customerService.GetAllCustomersAsync(1, 200);
                AvailableCustomers.Clear();
                if (customers != null)
                {
                    foreach (var c in customers) AvailableCustomers.Add(c);
                }

                // Load cars based on initial default dates
                await LoadAvailableCarsForSelectedDatesAsync();
            }
            catch (Exception ex) { MessageBox.Show($"Error loading initial data: {ex.Message}", "Load Error"); }
            finally { IsLoading = false; }
        }

        private async Task LoadAvailableCarsForSelectedDatesAsync()
        {
            // Ensure dates are valid before fetching
            if (StartDate.Date >= EndDate.Date)
            {
                AvailableCars.Clear();
                SelectedCar = null; // Clear selection if dates are invalid
                UpdateSummaryPanel();
                return;
            }

            IsLoadingCars = true; // Specific flag for car loading
            Car? previouslySelectedCar = SelectedCar; // Store current selection

            try
            {
                var cars = await _carService.GetAvailableCarsAsync(StartDate, EndDate);
                AvailableCars.Clear();
                if (cars != null)
                {
                    foreach (var c in cars) AvailableCars.Add(c);
                }

                // Try to re-select the previously selected car if it's still available
                if (previouslySelectedCar != null && AvailableCars.Any(c => c.Id == previouslySelectedCar.Id))
                {
                    SelectedCar = AvailableCars.First(c => c.Id == previouslySelectedCar.Id);
                }
                else if (AvailableCars.Any())
                {
                    // SelectedCar = AvailableCars.FirstOrDefault(); // Optionally select the first available
                    SelectedCar = null; // Or clear selection if previous one is no longer available
                }
                else
                {
                    SelectedCar = null; // No cars available for these dates
                }
            }
            catch (Exception ex)



            {
                MessageBox.Show($"Error loading available cars: {ex.Message}", "Car Load Error");
                AvailableCars.Clear();
                SelectedCar = null;
            }
            finally
            {
                IsLoadingCars = false;
                UpdateSummaryPanel(); // Update summary as car selection might have changed
                ValidateProperty(SelectedCar, nameof(SelectedCar)); // Re-validate SelectedCar
            }
        }

        // --- Custom Validation Methods ---
        public static ValidationResult? ValidatePaymentAmount(decimal amountPaid, ValidationContext context)
        {
            var instance = (AddNewRentalViewModel)context.ObjectInstance;
            if (instance.CustomerWillPayNow) // Only validate if the checkbox is checked
            {
                if (amountPaid <= 0)
                {
                    return new ValidationResult("Amount paid must be greater than 0 when paying now.", new[] { nameof(AmountPaid) });
                }
            }
            return ValidationResult.Success; // If not paying now, or if amount is > 0 when paying now
        }

        public static ValidationResult? ValidatePaymentMethod(string? paymentMethod, ValidationContext context)
        {
            var instance = (AddNewRentalViewModel)context.ObjectInstance;
            if (instance.CustomerWillPayNow && string.IsNullOrWhiteSpace(paymentMethod))
            {
                return new ValidationResult("Payment method is required when paying now.");
            }
            return ValidationResult.Success;
        }


        // --- Property Changed Handlers ---
        partial void OnCustomerWillPayNowChanged(bool value)
        {
            OnPropertyChanged(nameof(PaymentSectionVisibility));
            OnPropertyChanged(nameof(SummaryPaidNowVisibility));

            // When checkbox changes, re-validate payment fields as their requirement might change
            // Clearing errors first is important before re-validating.
            ClearErrors(nameof(AmountPaid));
            ClearErrors(nameof(SelectedPaymentMethod));
            ClearErrors(nameof(PaymentDate)); // Also clear PaymentDate errors

            if (value)
            {
                // Trigger validation for potentially now-required fields.
                // ValidateProperty will invoke the [CustomValidation] methods.
                ValidateProperty(AmountPaid, nameof(AmountPaid));
                ValidateProperty(SelectedPaymentMethod, nameof(SelectedPaymentMethod));
                ValidateProperty(PaymentDate, nameof(PaymentDate)); // Validate payment date as well
            }
            else
            {
                AmountPaid = 0; // This value is now valid if CustomerWillPayNow is false
                SelectedPaymentMethod = PaymentMethods.FirstOrDefault();
                // PaymentDate can remain DateTime.Today, or you can reset it if desired
                // Its validation (ValidatePaymentDate) will now pass if CustomerWillPayNow is false.
            }
            UpdateSummaryPanel();
        }

        partial void OnStartDateChanged(DateTime value)
        {
            if (value.Date >= EndDate.Date) EndDate = value.Date.AddDays(1);
            UpdateSummaryPanel();
            ValidateProperty(value, nameof(StartDate));
            ValidateProperty(EndDate, nameof(EndDate));
            _ = LoadAvailableCarsForSelectedDatesAsync(); // Reload cars when start date changes
        }

        partial void OnEndDateChanged(DateTime value)
        {
            if (value.Date <= StartDate.Date) StartDate = value.Date.AddDays(-1);
            UpdateSummaryPanel();
            ValidateProperty(value, nameof(EndDate));
            ValidateProperty(StartDate, nameof(StartDate));
            _ = LoadAvailableCarsForSelectedDatesAsync(); // Reload cars when end date changes
        }

        partial void OnSelectedCarChanged(Car? value) => UpdateSummaryPanel();
        partial void OnAmountPaidChanged(decimal value)
        {
            UpdateSummaryPanel();
            // Only validate AmountPaid on its own change if we are in "pay now" mode.
            // The [CustomValidation] attribute will handle this check when ValidateAllProperties or ValidateProperty is called.
            if (CustomerWillPayNow)
            {
                ValidateProperty(value, nameof(AmountPaid));
            }
            else
            {
                // If not paying now, an AmountPaid of 0 is valid, so clear any previous errors for it
                // that might have been set when CustomerWillPayNow was true.
                ClearErrors(nameof(AmountPaid));
            }
        }

        private void UpdateSummaryPanel()
        {
            if (SelectedCar == null || StartDate.Date >= EndDate.Date)
            {
                SummaryTotalAmount = 0;
            }
            else
            {
                TimeSpan rentalDuration = EndDate.Date - StartDate.Date;
                int numberOfDays = (int)Math.Max(1, rentalDuration.TotalDays + 1); // Inclusive day count
                SummaryTotalAmount = numberOfDays * SelectedCar.RentPerDay;
            }

            if (CustomerWillPayNow)
            {
                SummaryAmountPaid = AmountPaid;
                SummaryAdvanceOrDue = SummaryTotalAmount - AmountPaid;
                SummaryAdvanceOrDueLabel = (SummaryAdvanceOrDue >= 0) ? "Remaining Due" : "Change/Overpaid";
                if (SummaryAdvanceOrDue < 0) SummaryAdvanceOrDue *= -1; // Display as positive value
            }
            else
            {
                SummaryAmountPaid = 0;
                SummaryAdvanceOrDue = SummaryTotalAmount;
                SummaryAdvanceOrDueLabel = "Total Due";
            }
            OnPropertyChanged(nameof(SummaryTotalAmount));
            OnPropertyChanged(nameof(SummaryAmountPaid));
            OnPropertyChanged(nameof(SummaryAdvanceOrDue));
            OnPropertyChanged(nameof(SummaryAdvanceOrDueLabel));
            OnPropertyChanged(nameof(SummaryPaidNowVisibility));
        }

        [RelayCommand]
        private async Task SaveRentalAsync()
        {
            // Validate all properties based on their attributes (including custom ones)
            ValidateAllProperties(); // This will run all [Required], [Range], [DateGreaterThan], [CustomValidation]

            if (HasErrors)
            {

                MessageBox.Show("Please correct the validation errors displayed on the form.", "Validation Error");
                return;
            }

            // These should be caught by [Required] attributes and ValidateAllProperties
            if (SelectedCustomer == null || SelectedCar == null)
            {
                MessageBox.Show("Critical error: Customer or Car not selected despite passing validation. Please report this.", "Internal Error");
                return;
            }

            UpdateSummaryPanel(); // Ensure summary is absolutely final before creating objects

            var newRental = new RentalRecord
            {
                CustomerId = SelectedCustomer.ID,
                CarId = SelectedCar.Id,
                StartDate = this.StartDate,
                EndDate = this.EndDate,
                RentalArea = this.RentalArea,
                OtherInformation = this.OtherInformation,
                TotalDue = SummaryTotalAmount
            };

            PaymentRecord? paymentToSave = null;
            if (CustomerWillPayNow)
            {
                // If HasErrors is false here, it means ValidatePaymentAmount, etc., passed.
                paymentToSave = new PaymentRecord
                {
                    CustomerId = SelectedCustomer.ID,
                    // RentalId will be set by the service
                    AmountPaid = this.AmountPaid,
                    PaymentMethod = this.SelectedPaymentMethod,
                    PaymentDate = this.PaymentDate
                };
            }

            IsLoading = true;
            try
            {
                var newRentalId = await _rentalService.AddRentalAsync(newRental, paymentToSave);
                if (newRentalId.HasValue && newRentalId.Value > 0)
                {
                    MessageBox.Show($"New Rental (ID: {newRentalId.Value}) saved successfully!", "Success");
                    _navigationServiceFactory.Create<RentalsViewModel>().Navigate();
                }
                else
                {
                    MessageBox.Show("Failed to save rental. Please check logs or database constraints (e.g., unique keys, foreign keys, car availability).", "Save Error");
                }
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                if (ex.InnerException != null) errorMessage += $"\nInner Exception: {ex.InnerException.Message}";
                MessageBox.Show($"An error occurred: {errorMessage}", "Error");
            }
            finally { IsLoading = false; }
        }

        [RelayCommand]
        private void CloseForm() => _navigationServiceFactory.Create<RentalsViewModel>().Navigate();

        [RelayCommand]
        private void ClearForm()
        {
            StartDate = DateTime.Today;
            EndDate = DateTime.Today.AddDays(1);
            RentalArea = string.Empty;
            OtherInformation = string.Empty;
            SelectedCustomer = null;
            SelectedCar = null;
            CustomerWillPayNow = false; // This will trigger OnCustomerWillPayNowChanged to reset payment fields
            // AmountPaid, SelectedPaymentMethod, SelectedPaymentStatus, PaymentDate are reset within OnCustomerWillPayNowChanged or by it
            ClearErrors(); // Clears all validation errors
            UpdateSummaryPanel();
        }

        // --- Sidebar Navigation Commands ---
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