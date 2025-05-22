// NoorRAC/ViewModels/RentalsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection; // For IServiceProvider if EditRental needs it
using NoorRAC.Models;
using NoorRAC.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace NoorRAC.ViewModels
{
    public partial class RentalsViewModel : ObservableObject
    {
        private readonly App.NavigationServiceFactory _navigationServiceFactory;
        private readonly IRentalService _rentalService;
        private readonly IServiceProvider _serviceProvider; // For resolving EditRentalViewModel

        [ObservableProperty]
        private ObservableCollection<RentalDisplayRecord> _rentalRecords = new(); // Use RentalDisplayRecord

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SearchRentalsCommand))]
        private string? _searchText;

        // --- Pagination ---
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalRentalPages))]
        [NotifyCanExecuteChangedFor(nameof(NextRentalPageCommand))]
        [NotifyCanExecuteChangedFor(nameof(PreviousRentalPageCommand))]
        private int _currentRentalPage = 1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalRentalPages))]
        [NotifyCanExecuteChangedFor(nameof(NextRentalPageCommand))]
        private int _rentalPageSize = 15; // Or your preferred page size

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalRentalPages))]
        [NotifyCanExecuteChangedFor(nameof(NextRentalPageCommand))]
        private int _totalRentalCount;

        public int TotalRentalPages => TotalRentalCount > 0 ? (int)Math.Ceiling((double)TotalRentalCount / RentalPageSize) : 1;
        public string RentalPaginationText => $"Page {CurrentRentalPage} of {TotalRentalPages} (Total: {TotalRentalCount})";

        [ObservableProperty]
        private bool _isLoadingRentals;

        public RentalsViewModel(
            App.NavigationServiceFactory navigationServiceFactory,
            IRentalService rentalService,
            IServiceProvider serviceProvider)
        {
            _navigationServiceFactory = navigationServiceFactory;
            _rentalService = rentalService;
            _serviceProvider = serviceProvider;

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadRentalsAsync();
        }

        [RelayCommand]
        private async Task LoadRentalsAsync()
        {
            IsLoadingRentals = true;
            SearchRentalsCommand.NotifyCanExecuteChanged(); // In case it's bound to CanExecute
            PreviousRentalPageCommand.NotifyCanExecuteChanged();
            NextRentalPageCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(RentalPaginationText));
            try
            {
                TotalRentalCount = await _rentalService.GetTotalRentalCountAsync(SearchText); // Pass search text
                var fetchedRentals = await _rentalService.GetAllRentalsForDisplayAsync(CurrentRentalPage, RentalPageSize, SearchText);

                RentalRecords.Clear();
                if (fetchedRentals != null)
                {
                    foreach (var rental in fetchedRentals)
                    {
                        RentalRecords.Add(rental);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading rentals: {ex.Message}", "Load Error");
                RentalRecords.Clear();
                TotalRentalCount = 0;
            }
            finally
            {
                IsLoadingRentals = false;
                SearchRentalsCommand.NotifyCanExecuteChanged();
                PreviousRentalPageCommand.NotifyCanExecuteChanged();
                NextRentalPageCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(RentalPaginationText));
            }
        }

        // Search command now reloads data from server
        [RelayCommand(CanExecute = nameof(CanSearchOrFilter))]
        private async Task SearchRentalsAsync()
        {
            CurrentRentalPage = 1; // Reset to first page for new search
            await LoadRentalsAsync();
        }
        private bool CanSearchOrFilter() => !IsLoadingRentals;


        // Optional: If you have date filters on Rentals.xaml, bind them and call this on change
        // private async Task ApplyFiltersAndReloadAsync()
        // {
        //     CurrentRentalPage = 1;
        //     await LoadRentalsAsync();
        // }

        [RelayCommand]
        private void NavigateToAddNewRental() // Renamed from AddNewRental for clarity
        {
            _navigationServiceFactory.Create<AddNewRentalViewModel>().Navigate();
        }

        [RelayCommand]
        private async Task EditRentalAsync(RentalDisplayRecord? rentalToEdit) // Parameter is RentalDisplayRecord
        {
            if (rentalToEdit != null)
            {
                // TODO: Implement navigation to an EditRentalViewModel
                var editRentalVM = _serviceProvider.GetRequiredService<EditRentalViewModel>();
                await editRentalVM.LoadRentalAsync(rentalToEdit.Id); // Pass Rental ID
                _navigationServiceFactory.Create<EditRentalViewModel>(sp => editRentalVM).Navigate();
            }
        }

        // --- Pagination ---
        [RelayCommand(CanExecute = nameof(CanGoToPreviousRentalPage))]
        private async Task PreviousRentalPageAsync()
        {
            CurrentRentalPage--;
            await LoadRentalsAsync();
        }
        private bool CanGoToPreviousRentalPage() => CurrentRentalPage > 1 && !IsLoadingRentals;

        [RelayCommand(CanExecute = nameof(CanGoToNextRentalPage))]
        private async Task NextRentalPageAsync()
        {
            CurrentRentalPage++;
            await LoadRentalsAsync();
        }
        private bool CanGoToNextRentalPage() => CurrentRentalPage < TotalRentalPages && !IsLoadingRentals;


        // --- Sidebar Navigation ---
        [RelayCommand] private void NavigateToDashboard() => _navigationServiceFactory.Create<DashboardViewModel>().Navigate();
        [RelayCommand] private async Task NavigateToRentals() => await InitializeAsync(); // Refresh
        [RelayCommand] private void NavigateToCustomers() => _navigationServiceFactory.Create<CustomersViewModel>().Navigate();
        [RelayCommand] private void NavigateToPayments() => _navigationServiceFactory.Create<PaymentsViewModel>().Navigate();
        [RelayCommand] private void NavigateToExpenses() => _navigationServiceFactory.Create<ExpensesViewModel>().Navigate();
        [RelayCommand] private void NavigateToFinances() => _navigationServiceFactory.Create<FinancesViewModel>().Navigate();
        [RelayCommand] private void NavigateToCars() => _navigationServiceFactory.Create<CarsViewModel>().Navigate();
        [RelayCommand] private void Logout() => _navigationServiceFactory.Create<LoginViewModel>().Navigate();


        // Partial methods for CanExecute updates
        partial void OnIsLoadingRentalsChanged(bool value)
        {
            SearchRentalsCommand.NotifyCanExecuteChanged();
            PreviousRentalPageCommand.NotifyCanExecuteChanged();
            NextRentalPageCommand.NotifyCanExecuteChanged();
        }
        partial void OnCurrentRentalPageChanged(int value)
        {
            PreviousRentalPageCommand.NotifyCanExecuteChanged();
            NextRentalPageCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(RentalPaginationText));
        }
        partial void OnTotalRentalCountChanged(int value)
        {
            OnPropertyChanged(nameof(TotalRentalPages));
            NextRentalPageCommand.NotifyCanExecuteChanged();
            PreviousRentalPageCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(RentalPaginationText));
            if (CurrentRentalPage > TotalRentalPages && TotalRentalPages > 0) CurrentRentalPage = TotalRentalPages;
            else if (TotalRentalPages == 0) CurrentRentalPage = 1;
        }
    }
}