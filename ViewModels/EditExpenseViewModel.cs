// NoorRAC/ViewModels/EditExpenseViewModel.cs
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
    public partial class EditExpenseViewModel : ObservableValidator
    {
        private readonly App.NavigationServiceFactory _navigationServiceFactory;
        private readonly IExpenseService _expenseService;
        private readonly ICarService _carService;

        private int _expenseId; // ID of the expense being edited
        private bool _isDataLoading = false; // Prevents re-entrant calls during initial load

        [ObservableProperty]
        private string _pageTitle = "Edit Expense";

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
        private Car? _selectedCar;

        [ObservableProperty]
        private ObservableCollection<Car> _availableCars = new();

        [ObservableProperty]
        private bool _isLoading;

        // Error Message Properties
        public string? ExpenseDateErrorMessage => GetErrors(nameof(ExpenseDate)).FirstOrDefault()?.ErrorMessage;
        public string? AmountErrorMessage => GetErrors(nameof(Amount)).FirstOrDefault()?.ErrorMessage;
        public string? DescriptionErrorMessage => GetErrors(nameof(Description)).FirstOrDefault()?.ErrorMessage;

        public EditExpenseViewModel(
            App.NavigationServiceFactory navigationServiceFactory,
            IExpenseService expenseService,
            ICarService carService)
        {
            _navigationServiceFactory = navigationServiceFactory;
            _expenseService = expenseService;
            _carService = carService;

            ErrorsChanged += (s, e) => UpdateErrorMessages();
            // Data loading will be triggered by LoadExpenseDataAsync
        }

        private void UpdateErrorMessages()
        {
            OnPropertyChanged(nameof(ExpenseDateErrorMessage));
            OnPropertyChanged(nameof(AmountErrorMessage));
            OnPropertyChanged(nameof(DescriptionErrorMessage));
        }

        public async Task LoadExpenseDataAsync(int expenseIdToLoad)
        {
            if (_isDataLoading) return;
            _isDataLoading = true;
            IsLoading = true;
            _expenseId = expenseIdToLoad; // Store the ID

            // Reset fields before loading, in case the VM instance is reused
            ExpenseDate = DateTime.Today;
            Amount = 0;
            Description = string.Empty;
            SelectedCar = null;
            AvailableCars.Clear();
            ClearErrors();


            try
            {
                // 1. Load all available cars first
                var cars = await _carService.GetAllCarsWithDetailsAsync(); // Or relevant filter
                AvailableCars.Clear();
                if (cars != null)
                {
                    foreach (var car in cars) AvailableCars.Add(car);
                }
                // Optionally: Add a "No Car Associated" option if an expense might not have a car
                // AvailableCars.Insert(0, new CarRecord { Id = 0, DisplayNameWithRegNo = "-- No Associated Car --" });


                // 2. Load the specific expense to be edited
                var expenseToEdit = await _expenseService.GetExpenseByIdAsync(_expenseId);
                if (expenseToEdit == null)
                {
                    MessageBox.Show($"Expense with ID {_expenseId} not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    CloseForm(); // Navigate back or handle error
                    return;
                }

                // 3. Populate form fields from the loaded expense
                ExpenseDate = expenseToEdit.Date;
                Amount = expenseToEdit.Amount;
                Description = expenseToEdit.Description;

                // 4. Set the selected car
                if (expenseToEdit.CarId.HasValue)
                {
                    SelectedCar = AvailableCars.FirstOrDefault(c => c.Id == expenseToEdit.CarId.Value);
                }
                else
                {
                    // If "No Associated Car" option was added with Id = 0, select it:
                    // SelectedCar = AvailableCars.FirstOrDefault(c => c.Id == 0);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading expense details: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                _isDataLoading = false;
                ValidateAllProperties(); // Validate after loading data
                UpdateErrorMessages();
            }
        }

        [RelayCommand]
        private async Task SaveExpenseAsync()
        {
            ValidateAllProperties();
            if (HasErrors)
            {
                MessageBox.Show("Please correct the validation errors.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var expenseToUpdate = new ExpenseRecord
            {
                Id = _expenseId, // Critical: Use the stored ID for an update
                Date = this.ExpenseDate,
                Amount = this.Amount,
                Description = this.Description,
                CarId = SelectedCar?.Id == 0 ? null : SelectedCar?.Id // Handle "No Car" option if Id is 0
            };

            IsLoading = true;
            try
            {
                bool success = await _expenseService.UpdateExpenseAsync(expenseToUpdate);
                if (success)
                {
                    MessageBox.Show("Expense updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    _navigationServiceFactory.Create<ExpensesViewModel>().Navigate(); // Navigate back
                }
                else
                {
                    MessageBox.Show("Failed to update expense. Please check logs or try again.", "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
        private async Task DeleteExpenseAsync()
        {
            if (_expenseId == 0) return;

            var result = MessageBox.Show($"Are you sure you want to delete this expense (ID: {_expenseId})?",
                                         "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;
                try
                {
                    bool success = await _expenseService.DeleteExpenseAsync(_expenseId);
                    if (success)
                    {
                        MessageBox.Show("Expense deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        _navigationServiceFactory.Create<ExpensesViewModel>().Navigate(); // Navigate back
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete expense.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting expense: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }


        [RelayCommand]
        private void CloseForm()
        {
            _navigationServiceFactory.Create<ExpensesViewModel>().Navigate();
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