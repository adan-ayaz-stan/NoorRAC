// NoorRAC/ViewModels/CustomersViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NoorRAC.Models;
using NoorRAC.Services;
using System;
using System.Collections.ObjectModel;
// System.Linq is not strictly needed here anymore if search is server-side for the main list
using System.Threading.Tasks;
using System.Windows;

namespace NoorRAC.ViewModels
{
    public partial class CustomersViewModel : ObservableObject
    {
        private readonly App.NavigationServiceFactory _navigationServiceFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly ICustomerService _customerService;

        // _allCustomerRecords is no longer strictly needed if search is server-side for pagination
        // It was used for client-side filtering. We'll keep it for now if you want
        // to refine the current page further, but the primary list comes from server.
        // For a pure server-side search, CustomerRecords would be directly populated.
        private ObservableCollection<CustomerRecord> _allCustomerRecords;


        [ObservableProperty]
        private ObservableCollection<CustomerRecord> _customerRecords;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SearchCommand))] // SearchCommand might become async
        private string? _searchText;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalPages))]
        [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
        [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
        private int _currentPage = 1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalPages))]
        [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
        private int _pageSize = 10;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalPages))]
        [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
        private int _totalCustomerCount;

        public int TotalPages => TotalCustomerCount > 0 ? (int)Math.Ceiling((double)TotalCustomerCount / PageSize) : 1; // Ensure TotalPages is at least 1
        public string PaginationText => $"Page {CurrentPage} of {TotalPages} (Total: {TotalCustomerCount})";

        public CustomersViewModel(
            App.NavigationServiceFactory navigationServiceFactory,
            IServiceProvider serviceProvider,
            ICustomerService customerService)
        {
            _navigationServiceFactory = navigationServiceFactory;
            _serviceProvider = serviceProvider;
            _customerService = customerService;
            _allCustomerRecords = new ObservableCollection<CustomerRecord>();
            _customerRecords = new ObservableCollection<CustomerRecord>();
            _ = InitializeViewModelAsync();
        }

        private async Task InitializeViewModelAsync()
        {
            await PerformSearchAndLoadDataAsync();
        }

        // Unified method to load count and data based on current SearchText
        private async Task PerformSearchAndLoadDataAsync()
        {
            IsLoading = true;
            PreviousPageCommand.NotifyCanExecuteChanged();
            NextPageCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(PaginationText)); // Update text during load start

            try
            {
                // Get total count based on the current search term
                TotalCustomerCount = await _customerService.GetTotalCustomerCountAsync(SearchText);

                // Fetch the paged data based on the current search term and page
                var fetchedCustomers = await _customerService.GetAllCustomersAsync(CurrentPage, PageSize, SearchText);

                // Populate CustomerRecords directly from fetched data
                CustomerRecords.Clear(); // Clear the displayed list
                _allCustomerRecords.Clear(); // Also clear this if you keep it for other purposes

                if (fetchedCustomers != null)
                {
                    foreach (var cust in fetchedCustomers)
                    {
                        // If relying solely on server search for the list, populate CustomerRecords directly
                        CustomerRecords.Add(cust);
                        _allCustomerRecords.Add(cust); // Keep _allCustomerRecords if you need a local copy of the current page
                    }
                }
                // Client-side FilterCustomers is no longer strictly needed if server does the search for pagination
                // If you removed FilterCustomers, ensure CustomerRecords is directly populated above.
                // For simplicity, if FilterCustomers is still used, it would filter the current page results
                // but this is redundant if the server already filtered.
                // Let's assume for now that CustomerRecords is the direct server result for the page.
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load customer data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                CustomerRecords.Clear(); // Clear on error
                _allCustomerRecords.Clear();
                TotalCustomerCount = 0; // Reset count on error
            }
            finally
            {
                IsLoading = false;
                PreviousPageCommand.NotifyCanExecuteChanged();
                NextPageCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(PaginationText)); // Update text after load
            }
        }

        // Remove or repurpose FilterCustomers if search is purely server-side for the main list
        // For now, if it exists, it would filter the currently displayed page (CustomerRecords)
        // but the source _allCustomerRecords is now just a copy of the current page.
        /*
        private void FilterCustomers()
        {
            if (string.IsNullOrWhiteSpace(SearchText) || !_allCustomerRecords.Any())
            {
                CustomerRecords = new ObservableCollection<CustomerRecord>(_allCustomerRecords);
            }
            else
            {
                var searchTextLower = SearchText.ToLower();
                var filtered = _allCustomerRecords.Where(c =>
                        (c.Name?.ToLower().Contains(searchTextLower) ?? false) ||
                        (c.CNIC?.ToLower().Contains(searchTextLower) ?? false) ||
                        (c.ContactInfo?.ToLower().Contains(searchTextLower) ?? false)
                    ).ToList();
                CustomerRecords = new ObservableCollection<CustomerRecord>(filtered);
            }
        }
        */

        // SearchCommand will now trigger a server-side search and data reload
        [RelayCommand]
        private async Task Search() // Changed to async
        {
            CurrentPage = 1; // Reset to the first page for a new search
            await PerformSearchAndLoadDataAsync();
        }

        // OnSearchTextChanged can also trigger a new search if you want live search
        // However, this might be too chatty with the database.
        // A common approach is a search button or a delay after typing.
        // For now, SearchText is primarily used by the SearchCommand.
        /*
        partial void OnSearchTextChanged(string? value)
        {
            // Optionally, implement a debounce mechanism here if you want live server search
            // For now, rely on the Search button/command.
            // If you want immediate client-side filtering of the current page:
            // FilterCustomers();
        }
        */


        [RelayCommand]
        private void AddNewCustomer()
        {
            var addNewCustomerNav = _navigationServiceFactory.Create<AddNewCustomerViewModel>();
            addNewCustomerNav.Navigate();
        }

        [RelayCommand]
        private async Task EditCustomer(CustomerRecord? customerToEdit)
        {
            if (customerToEdit != null)
            {
                try
                {
                    // 1. Resolve EditCustomerViewModel instance using DI
                    var editCustomerVM = _serviceProvider.GetRequiredService<EditCustomerViewModel>();

                    // 2. Load the customer data into the EditCustomerViewModel
                    //    This should be an async call if it involves I/O (like database access)
                    await editCustomerVM.LoadCustomerAsync(customerToEdit.ID);

                    // 3. Navigate using a factory that can accept an existing instance
                    //    The lambda `sp => editCustomerVM` tells the factory to use this specific,
                    //    already-loaded instance instead of creating a new one.
                    //    This requires your App.NavigationServiceFactory.Create method to support this.
                    var navigationService = _navigationServiceFactory.Create<EditCustomerViewModel>(sp => editCustomerVM);
                    navigationService.Navigate();
                }
                catch (Exception ex)
                {
                    // Handle exceptions, e.g., if EditCustomerViewModel can't be resolved or LoadCustomerAsync fails
                    MessageBox.Show($"Error navigating to edit customer: {ex.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    // You might want to log the full exception (ex.ToString())
                }
            }
        }

        [RelayCommand] private void NavigateToDashboard() => _navigationServiceFactory.Create<DashboardViewModel>().Navigate();
        [RelayCommand] private void NavigateToRentals() => _navigationServiceFactory.Create<RentalsViewModel>().Navigate();

        [RelayCommand]
        private async Task NavigateToCustomers()
        {
            CurrentPage = 1;
            // SearchText = string.Empty; // Optionally clear search when navigating to customers
            await InitializeViewModelAsync();
        }

        [RelayCommand] private void NavigateToPayments() => _navigationServiceFactory.Create<PaymentsViewModel>().Navigate();
        [RelayCommand] private void NavigateToExpenses() => _navigationServiceFactory.Create<ExpensesViewModel>().Navigate();
        [RelayCommand] private void NavigateToFinances() => _navigationServiceFactory.Create<FinancesViewModel>().Navigate();
        [RelayCommand] private void NavigateToCars() => _navigationServiceFactory.Create<CarsViewModel>().Navigate();
        [RelayCommand] private void Logout() => _navigationServiceFactory.Create<LoginViewModel>().Navigate();

        [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
        private async Task PreviousPageAsync()
        {
            CurrentPage--;
            await PerformSearchAndLoadDataAsync(); // Reload data for the new page with current search term
        }
        private bool CanGoToPreviousPage() => CurrentPage > 1 && !IsLoading;

        [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
        private async Task NextPageAsync()
        {
            CurrentPage++;
            await PerformSearchAndLoadDataAsync(); // Reload data for the new page with current search term
        }
        private bool CanGoToNextPage() => CurrentPage < TotalPages && !IsLoading;

        partial void OnIsLoadingChanged(bool value)
        {
            SearchCommand.NotifyCanExecuteChanged(); // If SearchCommand becomes async and has CanExecute
            PreviousPageCommand.NotifyCanExecuteChanged();
            NextPageCommand.NotifyCanExecuteChanged();
        }
        partial void OnCurrentPageChanged(int value)
        {
            PreviousPageCommand.NotifyCanExecuteChanged();
            NextPageCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(PaginationText));
        }
        partial void OnTotalCustomerCountChanged(int value)
        {
            OnPropertyChanged(nameof(TotalPages));
            NextPageCommand.NotifyCanExecuteChanged();
            PreviousPageCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(PaginationText));
            // If current page becomes invalid due to new total count, adjust it
            if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;
            else if (TotalPages == 0) CurrentPage = 1; // Or handle no results state
        }
        partial void OnPageSizeChanged(int value)
        {
            OnPropertyChanged(nameof(TotalPages));
            CurrentPage = 1;
            _ = PerformSearchAndLoadDataAsync(); // Use the unified method
            NextPageCommand.NotifyCanExecuteChanged();
            PreviousPageCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(PaginationText));
        }
    }
}