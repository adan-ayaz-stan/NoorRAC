// NoorRAC/ViewModels/AddNewCustomerViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoorRAC.Models;
using NoorRAC.Services; // Your services namespace
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq; // For Select in error message concatenation
using System.Threading.Tasks; // For async
using System.Windows;

namespace NoorRAC.ViewModels
{
    public partial class AddNewCustomerViewModel : ObservableValidator
    {
        private readonly App.NavigationServiceFactory _navigationServiceFactory;
        private readonly ICustomerService _customerService; // Inject the service interface

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Customer name is required.")]
        [MinLength(3, ErrorMessage = "Name must be at least 3 characters long.")]
        private string? _customerName;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "CNIC is required.")]
        [RegularExpression(@"^\d{5}-\d{7}-\d{1}$", ErrorMessage = "CNIC must be in XXXXX-XXXXXXX-X format.")]
        private string? _cNIC; // Corrected from _cNIC to follow convention

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Contact information is required.")]
        private string? _contactInfo;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Address is required.")]
        private string? _address;

        // Constructor updated to inject ICustomerService
        public AddNewCustomerViewModel(
            App.NavigationServiceFactory navigationServiceFactory,
            ICustomerService customerService) // Inject ICustomerService
        {
            _navigationServiceFactory = navigationServiceFactory;
            _customerService = customerService; // Store the injected service

            // It's generally better to validate on demand (e.g., when Save is clicked or property changes)
            // rather than validating everything in the constructor, especially if properties start empty.
            // ValidateAllProperties();
        }

        [RelayCommand(CanExecute = nameof(CanSave))] // Optional: Add CanExecute
        private async Task SaveCustomerInformationAsync() // Renamed to Async
        {
            ClearErrors();
            ValidateAllProperties();

            if (HasErrors)
            {
                // Consider using a less intrusive way to show errors if inline validation is working
                string allErrors = string.Join(Environment.NewLine, GetErrors().Select(e => e.ErrorMessage));

                MessageBox.Show($"Please correct the following errors:\n{allErrors}", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                MessageBox.Show("Please correct the validation errors.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var newCustomer = new CustomerRecord
            {
                Name = this.CustomerName,
                CNIC = this.CNIC,
                ContactInfo = this.ContactInfo,
                Address = this.Address,
                DateJoined = DateTime.Now,
            };

            try
            {
                bool success = await _customerService.AddCustomerAsync(newCustomer);

                if (success)
                {
                    MessageBox.Show($"Customer '{newCustomer.Name}' saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    _navigationServiceFactory.Create<CustomersViewModel>().Navigate(); // Navigate back
                }
                else
                {
                    MessageBox.Show($"Failed to save customer '{newCustomer.Name}'. Please check logs or try again.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                // Log the exception (ex.ToString())
                MessageBox.Show($"An error occurred while saving the customer: {ex.Message}", "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Optional: CanExecute for the Save command
        private bool CanSave()
        {
            // Example: disable save if there are already known errors or if certain fields are empty
            // This complements the validation done inside the command.
            // return !HasErrors; // Simplest form
            return true; // Or more complex logic
        }


        [RelayCommand]
        private void CloseForm()
        {
            _navigationServiceFactory.Create<CustomersViewModel>().Navigate();
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