// NoorRAC/ViewModels/ExpensesViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NoorRAC.Models;
using NoorRAC.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows; // For MessageBox

namespace NoorRAC.ViewModels
{
    public partial class ExpensesViewModel : ObservableObject // Or ObservableValidator if validation needed on this VM
    {
        private readonly App.NavigationServiceFactory _navigationServiceFactory;
        private readonly IExpenseService _expenseService;
        private readonly IServiceProvider _serviceProvider; // For resolving Add/Edit ViewModels

        [ObservableProperty]
        private ObservableCollection<ExpenseDisplayRecord> _expensesRecords = new();

        [ObservableProperty]
        private DateTime? _fromDate;

        [ObservableProperty]
        private DateTime? _toDate = DateTime.Today; // Default to today

        [ObservableProperty]
        private string? _searchTerm = "Search expenses here..."; // Placeholder text

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _pageSize = 15; // Or your preferred page size

        [ObservableProperty]
        private int _totalItems;

        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

        public ExpensesViewModel(
            App.NavigationServiceFactory navigationServiceFactory,
            IExpenseService expenseService,
            IServiceProvider serviceProvider)
        {
            _navigationServiceFactory = navigationServiceFactory;
            _expenseService = expenseService;
            _serviceProvider = serviceProvider;

            // Trigger initial load
            _ = LoadExpensesAsync();
        }

        partial void OnFromDateChanged(DateTime? value) => _ = ApplyFiltersAndLoadAsync();
        partial void OnToDateChanged(DateTime? value) => _ = ApplyFiltersAndLoadAsync();

        private async Task ApplyFiltersAndLoadAsync()
        {
            CurrentPage = 1; // Reset to first page when filters change
            await LoadExpensesAsync();
        }

        [RelayCommand]
        private void ClearSearchTerm() // For TextBox GotFocus
        {
            if (SearchTerm == "Search expenses here...")
            {
                SearchTerm = string.Empty;
            }
        }

        [RelayCommand]
        private void RestoreSearchTermPlaceholder() // For TextBox LostFocus
        {
            if (string.IsNullOrWhiteSpace(SearchTerm))
            {
                SearchTerm = "Search expenses here...";
            }
        }


        [RelayCommand]
        private async Task SearchExpensesAsync()
        {
            CurrentPage = 1; // Reset to first page for new search
            await LoadExpensesAsync();
        }

        private async Task LoadExpensesAsync()
        {
            IsLoading = true;
            try
            {
                string? currentSearchTerm = (SearchTerm == "Search expenses here..." || string.IsNullOrWhiteSpace(SearchTerm)) ? null : SearchTerm;

                TotalItems = await _expenseService.GetTotalExpensesCountAsync(FromDate, ToDate, currentSearchTerm);
                var expenses = await _expenseService.GetAllExpensesAsync(CurrentPage, PageSize, FromDate, ToDate, currentSearchTerm);

                ExpensesRecords.Clear();
                if (expenses != null)
                {
                    foreach (var expense in expenses)
                    {
                        ExpensesRecords.Add(expense);
                    }
                }
                OnPropertyChanged(nameof(TotalPages)); // Notify UI about page count change
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading expenses: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
        private async Task PreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadExpensesAsync();
            }
        }
        private bool CanGoToPreviousPage() => CurrentPage > 1 && !IsLoading;

        [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
        private async Task NextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadExpensesAsync();
            }
        }
        private bool CanGoToNextPage() => CurrentPage < TotalPages && !IsLoading;


        [RelayCommand]
        private async Task AddNewExpenseAsync()
        {
            var addNewExpenseVM = _serviceProvider.GetRequiredService<AddNewExpenseViewModel>();
            // The LoadInitialDataAsync is already called in AddNewExpenseViewModel's constructor,
            // but if you want to ensure it's fresh upon navigation or pass parameters:
            //await addNewExpenseVM.LoadInitialDataAsync(); 

            _navigationServiceFactory.Create<AddNewExpenseViewModel>(sp => addNewExpenseVM).Navigate();
        }

        [RelayCommand]
        private async Task EditExpenseAsync(ExpenseDisplayRecord? expenseToEdit)
        {
            if (expenseToEdit == null) return;

            var editExpenseVM = _serviceProvider.GetRequiredService<EditExpenseViewModel>();
            await editExpenseVM.LoadExpenseDataAsync(expenseToEdit.Id); // Pass Expense ID to load data

            _navigationServiceFactory.Create<EditExpenseViewModel>(sp => editExpenseVM).Navigate();
        }

        [RelayCommand]
        private async Task DeleteExpenseAsync(ExpenseDisplayRecord? expenseToDelete)
        {
            if (expenseToDelete == null) return;

            var result = MessageBox.Show($"Are you sure you want to delete expense ID: {expenseToDelete.Id} ({expenseToDelete.Description})?",
                                         "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;
                try
                {
                    bool success = await _expenseService.DeleteExpenseAsync(expenseToDelete.Id);
                    if (success)
                    {
                        MessageBox.Show("Expense deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadExpensesAsync(); // Refresh the list
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


        // --- Sidebar Navigation Commands ---
        [RelayCommand] private void NavigateToDashboard() => _navigationServiceFactory.Create<DashboardViewModel>().Navigate();
        [RelayCommand] private void NavigateToRentals() => _navigationServiceFactory.Create<RentalsViewModel>().Navigate();
        [RelayCommand] private void NavigateToCustomers() => _navigationServiceFactory.Create<CustomersViewModel>().Navigate();
        [RelayCommand] private void NavigateToPayments() => _navigationServiceFactory.Create<PaymentsViewModel>().Navigate();
        [RelayCommand] private void NavigateToExpenses() { /* Already on this view */ }
        [RelayCommand] private void NavigateToFinances() => _navigationServiceFactory.Create<FinancesViewModel>().Navigate();
        [RelayCommand] private void NavigateToCars() => _navigationServiceFactory.Create<CarsViewModel>().Navigate();
        [RelayCommand] private void Logout() => _navigationServiceFactory.Create<LoginViewModel>().Navigate();
    }
}