// NoorRAC/ViewModels/RentalsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoorRAC.Models;
using NoorRAC.Services; // For INavigationService
using NoorRAC.Stores;   // For NavigationStore (if used directly, or via factory)
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows; // For MessageBox, consider a dedicated dialog service

namespace NoorRAC.ViewModels
{
    public partial class RentalsViewModel : ObservableObject
    {
        private readonly App.NavigationServiceFactory _navigationServiceFactory;
        private ObservableCollection<RentalRecord> _allRentalRecords; // To store all records for filtering

        [ObservableProperty]
        private ObservableCollection<RentalRecord> _rentalRecords;

        [ObservableProperty]
        private string? _searchText;

        public RentalsViewModel(App.NavigationServiceFactory navigationServiceFactory)
        {
            _navigationServiceFactory = navigationServiceFactory;
            _allRentalRecords = new ObservableCollection<RentalRecord>();
            _rentalRecords = new ObservableCollection<RentalRecord>();
            LoadSampleData();
        }

        private void LoadSampleData()
        {
            // Sample Data
            _allRentalRecords = new ObservableCollection<RentalRecord>
            {
                new RentalRecord { ID = 1, Name = "John Doe", CarType = "Sedan", CarNumber = "ABC-123", Status = "Finished", StartDate = DateTime.Now.AddDays(-10), EndDate = DateTime.Now.AddDays(-5) },
                new RentalRecord { ID = 2, Name = "Jane Smith", CarType = "SUV", CarNumber = "XYZ-789", Status = "Active", StartDate = DateTime.Now.AddDays(-2), EndDate = DateTime.Now.AddDays(5) },
                new RentalRecord { ID = 3, Name = "Alice Brown", CarType = "Hatchback", CarNumber = "DEF-456", Status = "Cancelled", StartDate = DateTime.Now.AddDays(1), EndDate = DateTime.Now.AddDays(3) },
                new RentalRecord { ID = 4, Name = "Bob White", CarType = "Sedan", CarNumber = "GHI-101", Status = "Finished", StartDate = DateTime.Now.AddMonths(-1), EndDate = DateTime.Now.AddMonths(-1).AddDays(7) },
                new RentalRecord { ID = 5, Name = "Charlie Green", CarType = "Van", CarNumber = "JKL-202", Status = "Active", StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(10) }
            };
            FilterRentals(); // Initial load
        }

        partial void OnSearchTextChanged(string? value)
        {
            FilterRentals();
        }

        private void FilterRentals()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                RentalRecords = new ObservableCollection<RentalRecord>(_allRentalRecords);
            }
            else
            {
                var searchTextLower = SearchText.ToLower();
                RentalRecords = new ObservableCollection<RentalRecord>(
                    _allRentalRecords.Where(r =>
                        (r.Name?.ToLower().Contains(searchTextLower) ?? false) ||
                        (r.CarType?.ToLower().Contains(searchTextLower) ?? false) ||
                        (r.CarNumber?.ToLower().Contains(searchTextLower) ?? false) ||
                        (r.Status?.ToLower().Contains(searchTextLower) ?? false)
                    )
                );
            }
        }

        [RelayCommand]
        private void Search()
        {
            // The filtering is now done automatically when SearchText changes.
            // This command can be used if you want an explicit search button press to trigger it,
            // or for more complex search logic. For now, it can be empty or log.
            MessageBox.Show($"Searching for: {SearchText ?? "everything"}", "Search Action");
            FilterRentals();
        }

        [RelayCommand]
        private void AddNewRental()
        {
            // Logic to open a new rental form/dialog
            // For example, navigate to an AddRentalViewModel
            // var addRentalNav = _navigationServiceFactory.Create<AddRentalViewModel>();
            // addRentalNav.Navigate();
            MessageBox.Show("Add New Rental clicked!", "Action");
        }

        [RelayCommand]
        private void EditRental(RentalRecord? rentalToEdit)
        {
            if (rentalToEdit != null)
            {
                // Logic to open an edit rental form/dialog, passing rentalToEdit
                // For example, navigate to an EditRentalViewModel
                // var editRentalVM = _serviceProvider.GetRequiredService<EditRentalViewModel>();
                // editRentalVM.LoadRental(rentalToEdit);
                // var editRentalNav = _navigationServiceFactory.Create<EditRentalViewModel>(); // This needs complex setup for passing params
                // A simpler way might be a modal dialog or a dedicated edit area.
                MessageBox.Show($"Editing Rental ID: {rentalToEdit.ID}, Client: {rentalToEdit.Name}", "Action");
            }
        }

        // --- Navigation Commands ---
        [RelayCommand]
        private void NavigateToDashboard()
        {
            _navigationServiceFactory.Create<DashboardViewModel>().Navigate();
        }

        [RelayCommand]
        private void NavigateToRentals()
        {
            // Already on Rentals, could be a refresh action
            LoadSampleData(); // Example: Refresh data
            MessageBox.Show("Refreshed Rentals data.", "Rentals");
        }

        [RelayCommand]
        private void NavigateToCustomers()
        {
            _navigationServiceFactory.Create<CustomersViewModel>().Navigate();
        }

        [RelayCommand]
        private void NavigateToPayments()
        {
            _navigationServiceFactory.Create<PaymentsViewModel>().Navigate();
        }

        [RelayCommand]
        private void NavigateToExpenses()
        {
            _navigationServiceFactory.Create<ExpensesViewModel>().Navigate();
        }

        [RelayCommand]
        private void NavigateToFinances()
        {
            _navigationServiceFactory.Create<FinancesViewModel>().Navigate();
        }

        [RelayCommand]
        private void NavigateToCars()
        {
            _navigationServiceFactory.Create<CarsViewModel>().Navigate();
        }

        [RelayCommand]
        private void Logout()
        {
            // Logic for logout: clear user session, navigate to Login
            // Example: Clear user session data if stored
            // _userStore.CurrentUser = null; 
            _navigationServiceFactory.Create<LoginViewModel>().Navigate();
        }
    }
}