// NoorRAC/ViewModels/AddNewExpenseViewModel.cs
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
    public partial class AddNewExpenseViewModel : ObservableValidator // Use ObservableValidator for validation
    {
        private readonly App.NavigationServiceFactory _navigationServiceFactory;
        private readonly IExpenseService _expenseService;
        private readonly ICarService _carService; // To populate the cars ComboBox

        [ObservableProperty]
        private string _pageTitle = "Add New Expense";

        // --- Form Properties for Expense ---
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Expense date is required.")]
        private DateTime _expenseDate = DateTime.Today;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, int.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        private int _amount;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Description is required.")]
        [MinLength(3, ErrorMessage = "Description must be at least 3 characters long.")]
        private string? _description;

        [ObservableProperty]
        private Car? _selectedCar; // Optional: Link to a car

        [ObservableProperty]
        private ObservableCollection<Car> _availableCars = new();

        [ObservableProperty]
        private bool _isLoading;

        // Error Message Properties for UI
        public string? ExpenseDateErrorMessage => GetErrors(nameof(ExpenseDate)).FirstOrDefault()?.ErrorMessage;
        public string? AmountErrorMessage => GetErrors(nameof(Amount)).FirstOrDefault()?.ErrorMessage;
        public string? DescriptionErrorMessage => GetErrors(nameof(Description)).FirstOrDefault()?.ErrorMessage;
        // No error for SelectedCar as it's optional

        public AddNewExpenseViewModel(
            App.NavigationServiceFactory navigationServiceFactory,
            IExpenseService expenseService,
            ICarService carService)
        {
            _navigationServiceFactory = navigationServiceFactory;
            _expenseService = expenseService;
            _carService = carService;

            ErrorsChanged += (s, e) => UpdateErrorMessages();
            _ = LoadInitialDataAsync(); // Fire and forget
        }

        private void UpdateErrorMessages()
        {
            OnPropertyChanged(nameof(ExpenseDateErrorMessage));
            OnPropertyChanged(nameof(AmountErrorMessage));
            OnPropertyChanged(nameof(DescriptionErrorMessage));
        }

        public async Task LoadInitialDataAsync() // Public if you need to call it externally upon navigation
        {
            IsLoading = true;
            try
            {
                // Reset fields to default
                ExpenseDate = DateTime.Today;
                Amount = 0;
                Description = string.Empty;
                SelectedCar = null;
                ClearErrors(); // Clear any previous validation errors

                var cars = await _carService.GetAllCarsWithDetailsAsync(); // Get all cars, or filter if needed
                AvailableCars.Clear();
                if (cars != null)
                {
                    foreach (var car in cars)
                    {
                        AvailableCars.Add(car);
                    }
                }
                // Optionally add a "No Car" or "General Expense" option
                // AvailableCars.Insert(0, new CarRecord { Id = 0, DisplayName = "-- No Specific Car --" });
                // SelectedCar = AvailableCars.FirstOrDefault();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading initial data: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }


        [RelayCommand]
        private async Task SaveExpenseAsync()
        {
            ValidateAllProperties(); // Trigger validation
            if (HasErrors)
            {
                MessageBox.Show("Please correct the validation errors.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var newExpense = new ExpenseRecord
            {
                Date = this.ExpenseDate,
                Amount = this.Amount,
                Description = this.Description,
                CarId = SelectedCar?.Id == 0 ? null : SelectedCar?.Id // Handle "No Car" option if Id is 0
            };

            IsLoading = true;
            try
            {
                bool success = await _expenseService.AddExpenseAsync(newExpense);
                if (success)
                {
                    MessageBox.Show("Expense added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    // Navigate back to the expenses list
                    _navigationServiceFactory.Create<ExpensesViewModel>().Navigate();
                }
                else
                {
                    MessageBox.Show("Failed to save expense. Please check logs or try again.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            // Navigate back to Expenses list
            _navigationServiceFactory.Create<ExpensesViewModel>().Navigate();
        }

        // --- Sidebar Navigation Commands (copy from other ViewModels if needed) ---
        // For brevity, I'll assume these are similar to other ViewModels
        [RelayCommand] private void NavigateToDashboard() => _navigationServiceFactory.Create<DashboardViewModel>().Navigate();
        [RelayCommand] private void NavigateToRentals() => _navigationServiceFactory.Create<RentalsViewModel>().Navigate();
        [RelayCommand] private void NavigateToCustomers() => _navigationServiceFactory.Create<CustomersViewModel>().Navigate();
        [RelayCommand] private void NavigateToPayments() => _navigationServiceFactory.Create<PaymentsViewModel>().Navigate();
        // Current view is Expenses context, so clicking Expenses in sidebar might do nothing or refresh
        [RelayCommand] private void NavigateToExpenses() => _navigationServiceFactory.Create<ExpensesViewModel>().Navigate();
        [RelayCommand] private void NavigateToFinances() => _navigationServiceFactory.Create<FinancesViewModel>().Navigate();
        [RelayCommand] private void NavigateToCars() => _navigationServiceFactory.Create<CarsViewModel>().Navigate();
        [RelayCommand] private void Logout() => _navigationServiceFactory.Create<LoginViewModel>().Navigate();
    }
}