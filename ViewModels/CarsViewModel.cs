// NoorRAC/ViewModels/CarsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoorRAC.Models;
using NoorRAC.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows; // For MessageBox
using Microsoft.Extensions.DependencyInjection; // For IServiceProvider if needed for EditCar/CarDetails VMs

namespace NoorRAC.ViewModels
{
    public partial class CarsViewModel : ObservableObject
    {
        private readonly App.NavigationServiceFactory _navigationServiceFactory;
        private readonly ICarService _carService;
        private readonly IServiceProvider _serviceProvider; // For resolving EditCarViewModel etc.

        // --- Overview Stats ---
        [ObservableProperty]
        private int _totalCarsStat;
        [ObservableProperty]
        private int _carsRentedStat;
        [ObservableProperty]
        private int _carsAvailableStat;

        // --- Car List ---
        [ObservableProperty]
        private ObservableCollection<CarDisplayItemViewModel> _carList = new();

        [ObservableProperty]
        private bool _isLoading;
        [ObservableProperty]
        private bool _isLoadingStats;

        public CarsViewModel(
            App.NavigationServiceFactory navigationServiceFactory,
            ICarService carService,
            IServiceProvider serviceProvider) // Inject IServiceProvider
        {
            _navigationServiceFactory = navigationServiceFactory;
            _carService = carService;
            _serviceProvider = serviceProvider;

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadCarOverviewStatsAsync();
            await LoadCarsAsync();
        }

        [RelayCommand]
        private async Task LoadCarOverviewStatsAsync()
        {
            IsLoadingStats = true;
            try
            {
                var stats = await _carService.GetCarOverviewStatsAsync();
                if (stats != null)
                {
                    TotalCarsStat = stats.TotalCars;
                    CarsRentedStat = stats.CarsRented;
                    CarsAvailableStat = stats.CarsAvailable;
                }
            }
            catch (Exception ex) { MessageBox.Show($"Error loading car stats: {ex.Message}", "Error"); }
            finally { IsLoadingStats = false; }
        }

        [RelayCommand]
        private async Task LoadCarsAsync()
        {
            IsLoading = true;
            try
            {
                var carsFromService = await _carService.GetAllCarsWithDetailsAsync();
                CarList.Clear();
                if (carsFromService != null)
                {
                    foreach (var carModel in carsFromService)
                    {
                        var carVM = new CarDisplayItemViewModel(carModel, HandleEditCar, HandleSeeMoreCar);
                        // TODO: Call carVM.UpdateRentalStatus(...) here after fetching actual rental data for each car
                        // This would involve another service call, possibly IRentalService.GetCarRentalSummary(carModel.Id)
                        // For now, placeholder status is set in CarDisplayItemViewModel constructor.
                        CarList.Add(carVM);
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show($"Error loading cars: {ex.Message}", "Error"); }
            finally { IsLoading = false; }
        }

        private async void HandleEditCar(Car carToEdit) // Changed to async void for fire-and-forget nav
        {
            if (carToEdit == null) return;

            // MessageBox.Show($"Placeholder: Navigate to Edit Car screen for {carToEdit.DisplayName}", "Edit Car");
            try
            {
                // Resolve EditCarViewModel instance using DI
                var editCarVM = _serviceProvider.GetRequiredService<EditCarViewModel>();

                // Load the car data into the EditCarViewModel
                await editCarVM.LoadCarAsync(carToEdit.Id);

                // Navigate using a factory that can accept an existing instance
                var navigationService = _navigationServiceFactory.Create<EditCarViewModel>(sp => editCarVM);
                navigationService.Navigate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error navigating to edit car: {ex.Message}", "Navigation Error");
            }
        }


        private void HandleSeeMoreCar(Car carDetails)
        {
            // Navigate to CarDetailsViewModel, passing carDetails.Id
            MessageBox.Show($"Placeholder: Navigate to Car Details screen for {carDetails.DisplayNameWithRegNo}", "See More");
            // Example (you'll need CarDetailsViewModel and its LoadCarDetailsAsync method):
            /*
            try
            {
                var carDetailsVM = _serviceProvider.GetRequiredService<CarDetailsViewModel>(); // Assuming CarDetailsViewModel exists
                await carDetailsVM.LoadCarDetailsAsync(carDetails.Id); // Assuming LoadCarDetailsAsync exists
                _navigationServiceFactory.Create<CarDetailsViewModel>(sp => carDetailsVM).Navigate();
            }
            catch(Exception ex) { MessageBox.Show($"Error navigating to car details: {ex.Message}");}
            */
        }


        [RelayCommand]
        private void NavigateToAddNewCar()
        {
            _navigationServiceFactory.Create<AddNewCarViewModel>().Navigate();
        }

        // --- Sidebar Navigation Commands ---
        [RelayCommand] private void NavigateToDashboard() => _navigationServiceFactory.Create<DashboardViewModel>().Navigate();
        [RelayCommand] private void NavigateToRentals() => _navigationServiceFactory.Create<RentalsViewModel>().Navigate();
        [RelayCommand] private void NavigateToCustomers() => _navigationServiceFactory.Create<CustomersViewModel>().Navigate();
        [RelayCommand] private void NavigateToPayments() => _navigationServiceFactory.Create<PaymentsViewModel>().Navigate();
        [RelayCommand] private void NavigateToExpenses() => _navigationServiceFactory.Create<ExpensesViewModel>().Navigate();
        [RelayCommand] private void NavigateToFinances() => _navigationServiceFactory.Create<FinancesViewModel>().Navigate();
        [RelayCommand] private void NavigateToCars() => _ = InitializeAsync(); // Refresh current view
        [RelayCommand] private void Logout() => _navigationServiceFactory.Create<LoginViewModel>().Navigate();
    }
}