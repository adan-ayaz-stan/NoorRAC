// NoorRAC/ViewModels/EditPaymentViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoorRAC.Models;
using NoorRAC.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace NoorRAC.ViewModels
{
    public partial class EditPaymentViewModel : ObservableValidator
    {
        private readonly App.NavigationServiceFactory _navigationServiceFactory;
        private readonly ICustomerService _customerService;
        private readonly IPaymentService _paymentService;
        private readonly IRentalService _rentalService;

        private int _paymentId; // Will be set by LoadPaymentDataAsync
        private bool _isDataLoading = false;

        [ObservableProperty]
        private string _paymentHeader = "Edit Payment";

        // --- Form Properties for Payment ---
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(typeof(decimal), "0.01", "1000000000", ErrorMessage = "Amount must be greater than 0.")]
        private decimal _amountPaid;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Payment method is required.")]
        private string? _selectedPaymentMethod;
        public ObservableCollection<string> PaymentMethods { get; } = new() { "Cash", "Card", "Online", "Other" };

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Payment date is required.")]
        private DateTime _paymentDate = DateTime.Today;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Customer is required.")]
        private CustomerRecord? _selectedCustomer;

        [ObservableProperty]
        private string? _paymentComment;

        [ObservableProperty]
        private RentalRecord? _selectedRental;

        [ObservableProperty]
        private int _remainingDueForRentalFromCustomer;

        [ObservableProperty]
        private ObservableCollection<CustomerRecord> _availableCustomers = new();

        [ObservableProperty]
        private ObservableCollection<RentalRecord> _customerActiveRentals = new();

        [ObservableProperty]
        private int _totalDueForCustomer;

        [ObservableProperty]
        private int _remainingDueForCustomer;

        [ObservableProperty]
        private int _totalPaidForCustomer;

        // Error Message Properties
        public string? AmountPaidErrorMessage => GetErrors(nameof(AmountPaid)).FirstOrDefault()?.ErrorMessage;
        public string? SelectedPaymentMethodErrorMessage => GetErrors(nameof(SelectedPaymentMethod)).FirstOrDefault()?.ErrorMessage;
        public string? PaymentDateErrorMessage => GetErrors(nameof(PaymentDate)).FirstOrDefault()?.ErrorMessage;
        public string? SelectedCustomerErrorMessage => GetErrors(nameof(SelectedCustomer)).FirstOrDefault()?.ErrorMessage;

        [ObservableProperty]
        private bool _isLoading;

        // Constructor no longer takes paymentId
        public EditPaymentViewModel(
            App.NavigationServiceFactory navigationServiceFactory,
            ICustomerService customerService,
            IPaymentService paymentService,
            IRentalService rentalService)
        {
            _navigationServiceFactory = navigationServiceFactory;
            _customerService = customerService;
            _paymentService = paymentService;
            _rentalService = rentalService;

            ErrorsChanged += (s, e) => UpdateErrorMessages();
            // Do NOT call data loading logic here, it will be triggered by LoadPaymentDataAsync
        }

        private void UpdateErrorMessages()
        {
            OnPropertyChanged(nameof(AmountPaidErrorMessage));
            OnPropertyChanged(nameof(SelectedPaymentMethodErrorMessage));
            OnPropertyChanged(nameof(PaymentDateErrorMessage));
            OnPropertyChanged(nameof(SelectedCustomerErrorMessage));
        }

        // New public method to load data
        public async Task LoadPaymentDataAsync(int paymentId)
        {
            if (_isDataLoading) return; // Prevent re-entrancy if called multiple times quickly

            _paymentId = paymentId; // Set the payment ID for this instance
            _isDataLoading = true;
            IsLoading = true;

            // Reset fields before loading new data, in case this VM instance is reused.
            AmountPaid = 0;
            SelectedPaymentMethod = PaymentMethods.FirstOrDefault();
            PaymentDate = DateTime.Today;
            PaymentComment = null;
            SelectedCustomer = null; // This will clear dependent fields via OnSelectedCustomerChanged guard
            AvailableCustomers.Clear();
            CustomerActiveRentals.Clear();
            SelectedRental = null;
            TotalDueForCustomer = 0;
            RemainingDueForCustomer = 0;
            TotalPaidForCustomer = 0;
            RemainingDueForRentalFromCustomer = 0;
            ClearErrors(); // Clear previous validation errors

            try
            {
                var paymentToEdit = await _paymentService.GetPaymentByIdAsync(_paymentId);
                if (paymentToEdit == null)
                {
                    MessageBox.Show($"Payment with ID {_paymentId} not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    // Optionally, navigate back or disable form
                    CloseForm(); // Or handle appropriately
                    return;
                }

                var customers = await _customerService.GetAllCustomersAsync(1, 1000); // Consider a more robust way if many customers
                if (customers != null)
                {
                    foreach (var customer in customers) AvailableCustomers.Add(customer);
                }

                // Populate form fields. Order is important to ensure dependent loads trigger correctly after primary data is set.
                AmountPaid = paymentToEdit.AmountPaid;
                SelectedPaymentMethod = PaymentMethods.Contains(paymentToEdit.PaymentMethod) ? paymentToEdit.PaymentMethod : PaymentMethods.FirstOrDefault();
                PaymentDate = paymentToEdit.PaymentDate;
                PaymentComment = paymentToEdit.Comment;

                var customerForPayment = AvailableCustomers.FirstOrDefault(c => c.ID == paymentToEdit.CustomerId);
                if (customerForPayment != null)
                {
                    // Temporarily disable _isDataLoading guard for these explicit loads during initialization
                    bool oldIsDataLoading = _isDataLoading;
                    _isDataLoading = true; // Keep it true to prevent event handlers from firing prematurely

                    await LoadPaymentsInfoForCustomerAsync(customerForPayment.CNIC);
                    await LoadRentalsForCustomerAsync(customerForPayment.ID);

                    SelectedCustomer = customerForPayment; // Set after loading its rentals

                    if (paymentToEdit.RentalId.HasValue)
                    {
                        var rentalToSelect = CustomerActiveRentals.FirstOrDefault(r => r.ID == paymentToEdit.RentalId.Value);
                        if (rentalToSelect != null)
                        {
                            await LoadRentalInformationAsync(rentalToSelect.ID); // Load info for this rental
                            SelectedRental = rentalToSelect; // Then set it
                        }
                    }
                    _isDataLoading = oldIsDataLoading; // Restore guard for event handlers
                }
                else
                {
                    MessageBox.Show($"Customer ID {paymentToEdit.CustomerId} for this payment not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading payment details: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                _isDataLoading = false; // Allow event handlers to run normally after initial load sequence.
                ValidateAllProperties();
                UpdateErrorMessages();
            }
        }

        partial void OnSelectedCustomerChanged(CustomerRecord? oldValue, CustomerRecord? newValue)
        {
            if (_isDataLoading) return;

            CustomerActiveRentals.Clear();
            SelectedRental = null;

            if (newValue != null)
            {
                _ = LoadPaymentsInfoForCustomerAsync(newValue.CNIC);
                _ = LoadRentalsForCustomerAsync(newValue.ID);
            }
            else
            {
                // Clear related financial info if customer is deselected
                TotalDueForCustomer = 0;
                RemainingDueForCustomer = 0;
                TotalPaidForCustomer = 0;
                RemainingDueForRentalFromCustomer = 0;
            }
        }

        partial void OnSelectedRentalChanged(RentalRecord? oldValue, RentalRecord? newValue)
        {
            if (_isDataLoading) return;

            if (newValue != null)
            {
                _ = LoadRentalInformationAsync(newValue.ID);
            }
            else
            {
                RemainingDueForRentalFromCustomer = 0; // Clear if no rental is selected
            }
        }

        private async Task LoadPaymentsInfoForCustomerAsync(string cnic)
        {
            if (string.IsNullOrEmpty(cnic) || _paymentService == null) return;
            // No IsLoading toggle here, assume parent (LoadPaymentDataAsync or event handler) manages overall IsLoading state
            try
            {
                TotalDueForCustomer = await _paymentService.GetTotalDueForCustomer(cnic);
                RemainingDueForCustomer = await _paymentService.GetRemainingDueForCustomer(cnic);
                TotalPaidForCustomer = TotalDueForCustomer - RemainingDueForCustomer;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading payment info for customer: {ex.Message}", "Load Error");
                // Reset to avoid showing stale data
                TotalDueForCustomer = 0; RemainingDueForCustomer = 0; TotalPaidForCustomer = 0;
            }
        }

        private async Task LoadRentalInformationAsync(int rentalId)
        {
            if (rentalId <= 0 || _paymentService == null || SelectedCustomer == null)
            {
                RemainingDueForRentalFromCustomer = 0; // Ensure it's cleared if conditions not met
                return;
            }
            try
            {
                RemainingDueForRentalFromCustomer = await _paymentService.GetRemainingDueForRental(rentalId, SelectedCustomer.ID);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading rental information: {ex.Message}", "Load Error");
                RemainingDueForRentalFromCustomer = 0; // Reset
            }
        }

        private async Task LoadRentalsForCustomerAsync(int customerId)
        {
            if (customerId <= 0 || _rentalService == null) return;
            // CustomerActiveRentals.Clear(); // Moved to caller (LoadPaymentDataAsync, OnSelectedCustomerChanged) to avoid clearing during initial linked rental selection
            try
            {
                var rentals = await _rentalService.GetRentalsByCustomerIdAsync(customerId, "All Time");
                // Preserve existing CustomerActiveRentals if any, clear and add if this is a fresh load for a *new* customer
                // For LoadPaymentDataAsync, it's already cleared. For OnSelectedCustomerChanged, it's also cleared.
                if (rentals != null)
                {
                    // If CustomerActiveRentals was not cleared by the caller, clear it now.
                    // This is typically handled by the calling context (e.g., OnSelectedCustomerChanged clears it)
                    // CustomerActiveRentals.Clear(); 
                    foreach (var rental in rentals) CustomerActiveRentals.Add(rental);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading rentals for customer: {ex.Message}", "Load Error");
                CustomerActiveRentals.Clear(); // Clear on error to avoid stale list
            }
        }

        [RelayCommand]
        private async Task SavePaymentAsync()
        {
            ClearErrors();
            ValidateAllProperties();

            if (HasErrors)
            {
                MessageBox.Show("Please correct the validation errors.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (_paymentId == 0) // Should not happen if LoadPaymentDataAsync was called
            {
                MessageBox.Show("Payment ID is missing. Cannot update.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var paymentToUpdate = new PaymentRecord
            {
                Id = _paymentId,
                CustomerId = SelectedCustomer!.ID,
                RentalId = SelectedRental?.ID,
                AmountPaid = this.AmountPaid,
                PaymentMethod = this.SelectedPaymentMethod!,
                PaymentDate = this.PaymentDate,
                Comment = this.PaymentComment
            };

            IsLoading = true;
            try
            {
                bool success = await _paymentService.UpdatePaymentAsync(paymentToUpdate);
                if (success)
                {
                    MessageBox.Show($"Payment ID {_paymentId} updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    _navigationServiceFactory.Create<PaymentsViewModel>().Navigate();
                }
                else
                {
                    MessageBox.Show("Failed to update payment. Please check logs or try again.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void CloseForm()
        {
            _navigationServiceFactory.Create<PaymentsViewModel>().Navigate();
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