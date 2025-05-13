// NoorRAC/ViewModels/EditCustomerViewModel.cs
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
    public partial class EditCustomerViewModel : ObservableValidator
    {
        private readonly App.NavigationServiceFactory _navigationServiceFactory;
        private readonly ICustomerService _customerService; // To fetch/update customer
        private readonly IRentalService _rentalService;   // To fetch rentals

        private CustomerRecord? _originalCustomer;

        [ObservableProperty]
        private int _customerId;

        // --- Editable Customer Properties ---
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Customer name is required.")]
        [MinLength(3, ErrorMessage = "Name must be at least 3 characters long.")]
        [NotifyPropertyChangedFor(nameof(CustomerNameErrorMessage))]
        [NotifyPropertyChangedFor(nameof(BreadcrumbCustomerName))]
        private string? _customerName;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "CNIC is required.")]
        [RegularExpression(@"^\d{5}-\d{7}-\d{1}$", ErrorMessage = "CNIC must be in XXXXX-XXXXXXX-X format.")]
        [NotifyPropertyChangedFor(nameof(CNICErrorMessage))] // Notify for error message property
        private string? _cNIC; // Corrected from _cNIC

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Contact information is required.")]
        [NotifyPropertyChangedFor(nameof(ContactInfoErrorMessage))]
        private string? _contactInfo;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Address is required.")]
        [NotifyPropertyChangedFor(nameof(AddressErrorMessage))]
        private string? _address;

        // --- Read-only Summary Properties ---
        [ObservableProperty]
        private int _totalRentalsSummary;

        [ObservableProperty]
        private string? _totalPaymentsSummary;

        [ObservableProperty]
        private string? _firstRentalDateSummary; // Will be derived or from Customer.DateJoined

        [ObservableProperty]
        private string? _latestRentalDateSummary; // Will be derived from rentals

        // --- Customer's Rentals ---
        [ObservableProperty]
        private ObservableCollection<RentalRecord> _customerRentalRecords;

        [ObservableProperty]
        private string _selectedRentalFilter = "This month";

        public string[] RentalFilterOptions { get; } = { "Previous week", "This week", "This month", "All time" };

        public string BreadcrumbCustomerName => _originalCustomer?.Name ?? "Edit Customer"; // Use loaded original name

        // Error Message Properties
        public string? CustomerNameErrorMessage =>
    GetErrors(nameof(CustomerName)).OfType<ValidationResult>().FirstOrDefault()?.ErrorMessage;

        public string? CNICErrorMessage =>
            GetErrors(nameof(CNIC)).OfType<ValidationResult>().FirstOrDefault()?.ErrorMessage;

        public string? ContactInfoErrorMessage =>
            GetErrors(nameof(ContactInfo)).OfType<ValidationResult>().FirstOrDefault()?.ErrorMessage;

        public string? AddressErrorMessage =>
            GetErrors(nameof(Address)).OfType<ValidationResult>().FirstOrDefault()?.ErrorMessage;



        public EditCustomerViewModel(
            App.NavigationServiceFactory navigationServiceFactory,
            ICustomerService customerService,
            IRentalService rentalService)
        {
            _navigationServiceFactory = navigationServiceFactory;
            _customerService = customerService;
            _rentalService = rentalService;
            _customerRentalRecords = new ObservableCollection<RentalRecord>();

            // Subscribe to ErrorsChanged to update custom error message properties
            ErrorsChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(CustomerNameErrorMessage));
                OnPropertyChanged(nameof(CNICErrorMessage));
                OnPropertyChanged(nameof(ContactInfoErrorMessage));
                OnPropertyChanged(nameof(AddressErrorMessage));
            };
        }

        public async Task LoadCustomerAsync(int customerId)
        {
            CustomerId = customerId;
            IsLoading = true; // Assuming an IsLoading property exists or you add one

            try
            {
                _originalCustomer = await _customerService.GetCustomerByIdAsync(customerId);

                if (_originalCustomer != null)
                {
                    CustomerName = _originalCustomer.Name; // Set observable property
                    CNIC = _originalCustomer.CNIC;         // Set observable property
                    ContactInfo = _originalCustomer.ContactInfo; // Set observable property
                    Address = _originalCustomer.Address;       // Set observable property

                    // Update Summary Data (can be directly from _originalCustomer or calculated)
                    TotalRentalsSummary = _originalCustomer.TotalRentals;
                    TotalPaymentsSummary = _originalCustomer.OutstandingPayments.ToString("C"); // Format as currency

                    // For FirstRentalDate, if DateJoined represents this, use it.
                    // Otherwise, you might need another query or logic.
                    FirstRentalDateSummary = _originalCustomer.DateJoined.ToString("yyyy-MM-dd");

                    await LoadCustomerRentalsAsync(); // This will also set LatestRentalDateSummary

                    ClearErrors();
                    ValidateAllProperties(); // Validate the loaded properties
                }
                else
                {
                    MessageBox.Show($"Customer with ID {customerId} not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    NavigateToCustomers();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading customer data: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // Log ex.ToString()
                NavigateToCustomers();
            }
            finally
            {
                IsLoading = false; // Reset loading state
                OnPropertyChanged(nameof(BreadcrumbCustomerName)); // Ensure breadcrumb updates after name is loaded
            }
        }

        partial void OnSelectedRentalFilterChanged(string value)
        {
            _ = LoadCustomerRentalsAsync();
        }

        private async Task LoadCustomerRentalsAsync()
        {
            if (CustomerId == 0) return; // Don't load if no customer ID yet

            CustomerRentalRecords.Clear();
            LatestRentalDateSummary = "N/A"; // Default

            try
            {
                var rentals = await _rentalService.GetRentalsByCustomerIdAsync(CustomerId, SelectedRentalFilter);
                if (rentals != null)
                {
                    foreach (var rental in rentals.OrderByDescending(r => r.StartDate)) // Display newest first
                    {
                        CustomerRentalRecords.Add(rental);
                    }
                    if (CustomerRentalRecords.Any())
                    {
                        // Assuming StartDate on RentalRecord is the rental date
                        LatestRentalDateSummary = CustomerRentalRecords.First().StartDate.ToString("yyyy-MM-dd");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading customer rentals: {ex.Message}", "Rental Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // Log ex.ToString()
            }
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveCustomerInformationAsync()
        {
            ClearErrors();
            ValidateAllProperties();

            if (HasErrors)
            {
                MessageBox.Show("Please correct the validation errors.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_originalCustomer == null)
            {
                MessageBox.Show("Cannot save. Original customer data not loaded.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Create an updated record or update the original one
            var updatedCustomer = new CustomerRecord
            {
                ID = _originalCustomer.ID,
                Name = this.CustomerName, // Use public properties
                CNIC = this.CNIC,
                ContactInfo = this.ContactInfo,
                Address = this.Address,
                DateJoined = _originalCustomer.DateJoined, // Assuming DateJoined is not editable here
                // TotalRentals and OutstandingPayments are calculated, not directly set by user on this form
                TotalRentals = _originalCustomer.TotalRentals,
                OutstandingPayments = _originalCustomer.OutstandingPayments
            };

            try
            {
                bool success = await _customerService.UpdateCustomerAsync(updatedCustomer);
                if (success)
                {
                    MessageBox.Show($"Customer '{updatedCustomer.Name}' updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    // Optionally, reload the original customer to reflect any DB-side changes after update
                    // await LoadCustomerAsync(updatedCustomer.ID);
                    NavigateToCustomers();
                }
                else
                {
                    MessageBox.Show("Failed to update customer.", "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating customer: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // Log ex.ToString()
            }
        }
        private bool CanSave() => !HasErrors && _originalCustomer != null;


        [RelayCommand]
        private async Task DeleteCustomerAsync() // Changed to async
        {
            if (_originalCustomer == null) return;

            if (MessageBox.Show($"Are you sure you want to delete customer '{_originalCustomer.Name}'? This action CANNOT be undone.",
                                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    bool success = await _customerService.DeleteCustomerAsync(_originalCustomer.ID);
                    if (success)
                    {
                        MessageBox.Show($"Customer '{_originalCustomer.Name}' deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        NavigateToCustomers();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete customer.", "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting customer: {ex.Message}", "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    // Log ex.ToString()
                }
            }
        }

        [RelayCommand]
        private void CloseForm()
        {
            NavigateToCustomers();
        }

        // --- Sidebar Navigation Commands ---
        [RelayCommand] private void NavigateToDashboard() => _navigationServiceFactory.Create<DashboardViewModel>().Navigate();
        [RelayCommand] private void NavigateToRentals() => _navigationServiceFactory.Create<RentalsViewModel>().Navigate();
        [RelayCommand] private void NavigateToCustomers() => _navigationServiceFactory.Create<CustomersViewModel>().Navigate(); // This should take user back to the list
        [RelayCommand] private void NavigateToPayments() => _navigationServiceFactory.Create<PaymentsViewModel>().Navigate();
        [RelayCommand] private void NavigateToExpenses() => _navigationServiceFactory.Create<ExpensesViewModel>().Navigate();
        [RelayCommand] private void NavigateToFinances() => _navigationServiceFactory.Create<FinancesViewModel>().Navigate();
        [RelayCommand] private void NavigateToCars() => _navigationServiceFactory.Create<CarsViewModel>().Navigate();
        [RelayCommand] private void Logout() => _navigationServiceFactory.Create<LoginViewModel>().Navigate();


        // Example IsLoading property (add if not present)
        [ObservableProperty]
        private bool _isLoading;
    }
}