// NoorRAC/ViewModels/PaymentsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NoorRAC.Models; // Assuming PaymentDisplayRecord and PaymentOverviewStats models
using NoorRAC.Services; // Assuming IPaymentService
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows; // For MessageBox - consider a dialog service for better testability

namespace NoorRAC.ViewModels
{
    public partial class PaymentsViewModel : ObservableObject
    {
        private readonly App.NavigationServiceFactory _navigationServiceFactory;
        private readonly IPaymentService _paymentService;
        private readonly IServiceProvider _serviceProvider;

        // --- Observable Properties for Overview Stats ---
        [ObservableProperty] private decimal _overviewTotalPayments;
        [ObservableProperty] private decimal _overviewTotalReceived;
        [ObservableProperty] private decimal _overviewTotalDue;
        [ObservableProperty] private string _overviewPercentageChange = "0%";

        // --- Observable Properties for Filtering & Search ---
        [ObservableProperty] private DateTime _filterFromDate;
        [ObservableProperty] private DateTime _filterToDate;
        [ObservableProperty] private string? _searchText; // Bound to Search TextBox

        // --- Observable Properties for DataGrid and Pagination ---
        [ObservableProperty]
        private ObservableCollection<PaymentDisplayRecord> _paymentsDisplayRecords = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(NextPaymentPageCommand))]
        [NotifyCanExecuteChangedFor(nameof(PreviousPaymentPageCommand))]
        private int _currentPaymentPage = 1;

        [ObservableProperty] private int _paymentPageSize = 15; // Default page size

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(NextPaymentPageCommand))]
        [NotifyCanExecuteChangedFor(nameof(PreviousPaymentPageCommand))] // Also depends on total count
        private int _totalPaymentCount;

        public int TotalPaymentPages => TotalPaymentCount > 0 ? (int)Math.Ceiling((double)TotalPaymentCount / PaymentPageSize) : 1;

        // --- Observable Properties for UI State ---
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
        [NotifyCanExecuteChangedFor(nameof(PreviousPaymentPageCommand))]
        [NotifyCanExecuteChangedFor(nameof(NextPaymentPageCommand))]
        private bool _isLoading; // For ProgressBar

        public PaymentsViewModel(
            App.NavigationServiceFactory navigationServiceFactory,
            IPaymentService paymentService,
            IServiceProvider serviceProvider)
        {
            _navigationServiceFactory = navigationServiceFactory;
            _paymentService = paymentService;
            _serviceProvider = serviceProvider;

            // Initialize non-nullable date properties to "This Month"
            DateTime today = DateTime.Today;
            _filterFromDate = new DateTime(today.Year, today.Month, 1);
            _filterToDate = _filterFromDate.AddMonths(1).AddDays(-1);

            _ = InitializeViewAsync();
        }

        private async Task InitializeViewAsync()
        {
            // Load initial stats and payments based on default dates
            // IsLoading will be handled by LoadPaymentsAsync
            await LoadOverviewStatsAsync();
            await LoadPaymentsAsync();
        }

        private async Task LoadOverviewStatsAsync()
        {
            try
            {
                var stats = await _paymentService.GetPaymentOverviewStatsAsync(FilterFromDate, FilterToDate);
                if (stats != null)
                {
                    OverviewTotalPayments = stats.TotalPayments;
                    OverviewTotalReceived = stats.TotalReceivedPayments;
                    OverviewTotalDue = stats.TotalOutstandingDueOnRentals;
                    OverviewPercentageChange = $"{(stats.PercentageChangeFromLastPeriod >= 0 ? "+" : "")}{stats.PercentageChangeFromLastPeriod:F0}%";
                }
                else
                {
                    ResetOverviewStats();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading overview stats: {ex.Message}", "Error");
                ResetOverviewStats();
            }
        }

        private void ResetOverviewStats()
        {
            OverviewTotalPayments = 0;
            OverviewTotalReceived = 0;
            OverviewTotalDue = 0;
            OverviewPercentageChange = "N/A";
        }

        private async Task LoadPaymentsAsync()
        {
            IsLoading = true;
            try
            {
                // The last 'null' parameter assumes the service method previously took an
                // overviewFilter string which is no longer supplied from this UI.
                TotalPaymentCount = await _paymentService.GetTotalPaymentCountAsync(FilterFromDate, FilterToDate, SearchText, null);
                var payments = await _paymentService.GetAllPaymentsAsync(CurrentPaymentPage, PaymentPageSize, FilterFromDate, FilterToDate, SearchText, null);

                PaymentsDisplayRecords.Clear();
                if (payments != null)
                {
                    foreach (var payment in payments)
                    {
                        PaymentsDisplayRecords.Add(payment);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading payments: {ex.Message}", "Error");
                PaymentsDisplayRecords.Clear();
                TotalPaymentCount = 0;
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Combined reload helper
        private async Task ReloadAllDataAsync()
        {
            IsLoading = true; // Set loading for the combined operation
            try
            {
                CurrentPaymentPage = 1; // Reset to first page for new filter criteria
                await LoadOverviewStatsAsync();
                await LoadPaymentsAsync(); // This will also set IsLoading = false at its end
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reloading data: {ex.Message}", "Error");
            }
            finally
            {
                // If LoadPaymentsAsync doesn't run due to an error in LoadOverviewStatsAsync,
                // ensure IsLoading is reset.
                if (IsLoading) IsLoading = false;
            }
        }


        // --- Partial OnChanged methods for properties ---
        partial void OnFilterFromDateChanged(DateTime oldValue, DateTime newFromDate)
        {
            if (oldValue == newFromDate) return;

            if (_filterToDate < newFromDate) // Access backing field to check
            {
                FilterToDate = newFromDate; // This will trigger OnFilterToDateChanged, which will then call ReloadAllDataAsync
            }
            else
            {
                _ = ReloadAllDataAsync();
            }
        }

        partial void OnFilterToDateChanged(DateTime oldValue, DateTime newToDate)
        {
            if (oldValue == newToDate) return;

            if (_filterFromDate > newToDate) // Access backing field to check
            {
                FilterFromDate = newToDate; // This will trigger OnFilterFromDateChanged, which might adjust FilterToDate or call ReloadAllDataAsync
            }
            else
            {
                _ = ReloadAllDataAsync();
            }
        }

        // SearchText has UpdateSourceTrigger=PropertyChanged.
        // If you want live search as user types:
        // partial void OnSearchTextChanged(string? oldValue, string? newValue)
        // {
        //     if (oldValue != newValue)
        //     {
        //         // Optional: Add a debounce here if search is expensive
        //         CurrentPaymentPage = 1;
        //         _ = LoadPaymentsAsync(); // Or ReloadAllDataAsync if stats should also update with search term
        //     }
        // }
        // For now, search is handled by the SearchCommand button.

        // --- Commands ---
        [RelayCommand(CanExecute = nameof(CanSearch))]
        private async Task Search() // Bound to SearchButton
        {
            CurrentPaymentPage = 1;
            await LoadPaymentsAsync(); // Reloads payments with current SearchText and date filters
        }
        private bool CanSearch() => !IsLoading;

        // In your ViewModel that lists payments (e.g., PaymentsViewModel.cs)
        // Make sure _navigationServiceFactory is injected and available.
        // private readonly App.NavigationServiceFactory _navigationServiceFactory;

        [RelayCommand]
        private async Task EditPayment(PaymentDisplayRecord? paymentToEdit) // Bound to Edit button in DataGrid
        {
            if (paymentToEdit != null)
            {
                // Navigate to EditPaymentViewModel, passing the ID of the payment to edit.
                var editPaymentVM = _serviceProvider.GetRequiredService<EditPaymentViewModel>();
                await editPaymentVM.LoadPaymentDataAsync(paymentToEdit.Id); // Pass Rental ID
                _navigationServiceFactory.Create<EditPaymentViewModel>(sp => editPaymentVM).Navigate();

                // No need for MessageBox or await Task.CompletedTask if navigation is the only action.
            }
        }

        [RelayCommand]
        private void NavigateToAddNewPayment() => _navigationServiceFactory.Create<AddNewPaymentViewModel>().Navigate();

        // --- Pagination Commands ---
        [RelayCommand(CanExecute = nameof(CanGoToPreviousPaymentPage))]
        private async Task PreviousPaymentPage()
        {
            CurrentPaymentPage--;
            await LoadPaymentsAsync();
        }
        private bool CanGoToPreviousPaymentPage() => CurrentPaymentPage > 1 && !IsLoading;

        [RelayCommand(CanExecute = nameof(CanGoToNextPaymentPage))]
        private async Task NextPaymentPage()
        {
            CurrentPaymentPage++;
            await LoadPaymentsAsync();
        }
        private bool CanGoToNextPaymentPage() => CurrentPaymentPage < TotalPaymentPages && !IsLoading;

        // --- Sidebar Navigation Commands ---
        [RelayCommand] private void NavigateToDashboard() => _navigationServiceFactory.Create<DashboardViewModel>().Navigate();
        [RelayCommand] private void NavigateToRentals() => _navigationServiceFactory.Create<RentalsViewModel>().Navigate();
        [RelayCommand] private void NavigateToCustomers() => _navigationServiceFactory.Create<CustomersViewModel>().Navigate();
        [RelayCommand] private async Task NavigateToPayments() => await InitializeViewAsync(); // Refreshes current view
        [RelayCommand] private void NavigateToExpenses() => _navigationServiceFactory.Create<ExpensesViewModel>().Navigate();
        [RelayCommand] private void NavigateToFinances() => _navigationServiceFactory.Create<FinancesViewModel>().Navigate();
        [RelayCommand] private void NavigateToCars() => _navigationServiceFactory.Create<CarsViewModel>().Navigate();
        [RelayCommand] private void Logout() => _navigationServiceFactory.Create<LoginViewModel>().Navigate();

        // --- PropertyChanged Handlers for Command CanExecute updates and derived properties ---
        // IsLoading, CurrentPaymentPage, TotalPaymentCount have [NotifyCanExecuteChangedFor] attributes where needed.

        partial void OnTotalPaymentCountChanged(int oldValue, int newValue)
        {
            OnPropertyChanged(nameof(TotalPaymentPages)); // Notify that derived property changed

            if (CurrentPaymentPage > TotalPaymentPages && TotalPaymentPages > 0)
            {
                CurrentPaymentPage = TotalPaymentPages;
                _ = LoadPaymentsAsync(); // Reload if page was adjusted
            }
            else if (TotalPaymentPages == 0 && CurrentPaymentPage != 1)
            {
                CurrentPaymentPage = 1;
                // PaymentsDisplayRecords should be empty, LoadPaymentsAsync would confirm this if called.
                // Or ensure PaymentsDisplayRecords is cleared when TotalPaymentCount is 0.
                if (PaymentsDisplayRecords.Count > 0) PaymentsDisplayRecords.Clear();
            }
        }
    }
}