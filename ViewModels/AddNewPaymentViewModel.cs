// NoorRAC/ViewModels/AddNewPaymentViewModel.cs
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
    public partial class AddNewPaymentViewModel : ObservableValidator
    {
        private readonly App.NavigationServiceFactory _navigationServiceFactory;
        private readonly ICustomerService _customerService;
        private readonly IPaymentService _paymentService;
        private readonly IRentalService _rentalService; // If you still want to link to rentals

        // --- Form Properties for Payment ---
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, int.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        private int _amountPaid;

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

        // Optional: Link to an existing rental
        [ObservableProperty]
        private RentalRecord? _selectedRental;

        [ObservableProperty]
        private int _remainingDueForRentalFromCustomer;

        [ObservableProperty]
        private ObservableCollection<CustomerRecord> _availableCustomers = new();

        [ObservableProperty]
        private ObservableCollection<RentalRecord> _customerActiveRentals = new(); // To populate rental ComboBox

        [ObservableProperty]
        private int _totalDueForCustomer;

        [ObservableProperty]
        private int _remainingDueForCustomer;

        [ObservableProperty]
        private int _totalPaidForCustomer;


        // Error Message Properties
        public string? AmountPaidErrorMessage =>
        (GetErrors(nameof(AmountPaid)).FirstOrDefault() as ValidationResult)?.ErrorMessage;

        public string? SelectedPaymentMethodErrorMessage =>
    (GetErrors(nameof(SelectedPaymentMethod)).FirstOrDefault() as ValidationResult)?.ErrorMessage;

        public string? PaymentDateErrorMessage =>
            (GetErrors(nameof(PaymentDate)).FirstOrDefault() as ValidationResult)?.ErrorMessage;

        public string? SelectedCustomerErrorMessage =>
            (GetErrors(nameof(SelectedCustomer)).FirstOrDefault() as ValidationResult)?.ErrorMessage;

        // No error message for SelectedRental as it's optional for this simplified version

        [ObservableProperty]
        private bool _isLoading;

        public AddNewPaymentViewModel(
            App.NavigationServiceFactory navigationServiceFactory,
            ICustomerService customerService,
            IPaymentService paymentService,
            IRentalService rentalService )
        {
            _navigationServiceFactory = navigationServiceFactory;
            _customerService = customerService;
            _paymentService = paymentService;
             _rentalService = rentalService;

            SelectedPaymentMethod = PaymentMethods.FirstOrDefault();

            ErrorsChanged += (s, e) => UpdateErrorMessages();
            _ = LoadInitialDataAsync();
        }

        private void UpdateErrorMessages()
        {
            OnPropertyChanged(nameof(AmountPaidErrorMessage));
            OnPropertyChanged(nameof(SelectedPaymentMethodErrorMessage));
            OnPropertyChanged(nameof(PaymentDateErrorMessage));
            OnPropertyChanged(nameof(SelectedCustomerErrorMessage));
        }

        private async Task LoadInitialDataAsync()
        {
            IsLoading = true;
            try
            {
                var customers = await _customerService.GetAllCustomersAsync(1, 100); // Load customers
                AvailableCustomers.Clear();
                if (customers != null)
                {
                    foreach (var customer in customers) AvailableCustomers.Add(customer);
                }
                // SelectedCustomer = AvailableCustomers.FirstOrDefault(); // Don't auto-select, let user choose
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading customers: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // When a customer is selected, load their active/unpaid rentals (optional)
        partial void OnSelectedCustomerChanged(CustomerRecord? value)
        {
            CustomerActiveRentals.Clear();
            SelectedRental = null;
            if (value != null)
            {
                // TODO: If linking to rentals is desired:
                _ = LoadRentalsForCustomerAsync(value.ID);
                _ = LoadPaymentsInfoForCustomerAsync(value.CNIC);
            }
        }

        partial void OnSelectedRentalChanged(RentalRecord? value)
        {
            if (value != null)
            {
                // Load rental information if needed
                _ = LoadRentalInformationAsync(value.ID);
            }
        }

        private async Task LoadPaymentsInfoForCustomerAsync(string cnic)
        {
            if (_paymentService == null) return; // Guard if service not injected/needed
            IsLoading = true;
            try
            {
                TotalDueForCustomer = await _paymentService.GetTotalDueForCustomer(cnic);
                RemainingDueForCustomer = await _paymentService.GetRemainingDueForCustomer(cnic);
                TotalPaidForCustomer = TotalDueForCustomer - RemainingDueForCustomer;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading payment info for customer: {ex.Message}", "Load Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadRentalInformationAsync(int rentalId)
        {
            if (_rentalService == null) return; // Guard if service not injected/needed
            IsLoading = true;
            try
            {
                var dueRemainingOnRental = _paymentService.GetRemainingDueForRental(rentalId, SelectedCustomer!.ID);
                if (dueRemainingOnRental != null)
                {
                    RemainingDueForRentalFromCustomer = await dueRemainingOnRental;

                }
                else
                {
                    MessageBox.Show("No rental information found for the selected rental.", "Load Error");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading rental information: {ex.Message}", "Load Error");
            }
            finally
            {
                IsLoading = false;
            }
        }


        // Example method if you want to load rentals for the selected customer
        private async Task LoadRentalsForCustomerAsync(int customerId)
        {
            if (_rentalService == null) return; // Guard if service not injected/needed
            IsLoading = true;
            try
            {
                // You'd need a method in IRentalService to get e.g., active or unpaid rentals
                var rentals = await _rentalService.GetRentalsByCustomerIdAsync(customerId, "All Time");
                CustomerActiveRentals.Clear();
                if (rentals != null)
                {
                    foreach (var rental in rentals) CustomerActiveRentals.Add(rental);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading rentals for customer: {ex.Message}", "Load Error");
            }
            finally
            {
                IsLoading = false;
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

            var newPayment = new PaymentRecord
            {
                CustomerId = SelectedCustomer!.ID, // Not null due to [Required] validation
                RentalId = SelectedRental?.ID,    // Optional: Link to a rental if one is selected
                AmountPaid = this.AmountPaid,
                PaymentMethod = this.SelectedPaymentMethod,
                PaymentDate = this.PaymentDate,
                Comment = this.PaymentComment
            };

            IsLoading = true;
            try
            {
                bool success = await _paymentService.AddPaymentAsync(newPayment);
                if (success)
                {
                    MessageBox.Show($"Payment of {newPayment.AmountPaid:C} for {SelectedCustomer.Name} saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    // Navigate to a payments list or customer details view
                    _navigationServiceFactory.Create<PaymentsViewModel>().Navigate(); // Assuming a PaymentsViewModel exists
                }
                else
                {
                    MessageBox.Show("Failed to save payment. Please check logs or try again.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            // Navigate back to Payments list or Dashboard/Rentals
            _navigationServiceFactory.Create<PaymentsViewModel>().Navigate(); // Or appropriate view
        }

        // --- Sidebar Navigation Commands (Copied from previous, ensure they are relevant) ---
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