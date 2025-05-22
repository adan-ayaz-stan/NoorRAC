// NoorRAC/ViewModels/DashboardViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView; // Add this
using LiveChartsCore.SkiaSharpView.Painting; // For SolidColorPaint
using LiveChartsCore.SkiaSharpView.WPF; // For specific WPF components if needed later
using SkiaSharp;
using NoorRAC.Models;
using NoorRAC.Services;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows; // For MessageBox, if needed

namespace NoorRAC.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly App.NavigationServiceFactory _navigationServiceFactory;
        private readonly IDashboardService _dashboardService;

        // --- Properties for Financial Graph (e.g., Income vs Expense over last 6 months) ---
        [ObservableProperty]
        private ISeries[] _financialSeries = Array.Empty<ISeries>(); // Initialize to empty

        [ObservableProperty]
        private Axis[] _financialXAxes = { new Axis { Name = "Month", LabelsRotation = 15 } }; // Example X-axis

        [ObservableProperty]
        private Axis[] _financialYAxes = { new Axis { Name = "Amount (Rs.)", MinLimit = 0 } }; // Example Y-axis

        // --- Properties for Monthly Rental Graph (e.g., Number of rentals per month for last 6 months) ---
        [ObservableProperty]
        private ISeries[] _monthlyRentalSeries = Array.Empty<ISeries>();

        [ObservableProperty]
        private Axis[] _monthlyRentalXAxes = { new Axis { Name = "Month", LabelsRotation = 15 } };

        [ObservableProperty]
        private Axis[] _monthlyRentalYAxes = { new Axis { Name = "Number of Rentals", MinLimit = 0, MinStep = 1 } };

        [ObservableProperty]
        private DashboardStats _financialStats = new DashboardStats();

        [ObservableProperty]
        private ObservableCollection<FleetSummaryCar> _fleetSummary = new();

        [ObservableProperty]
        private ObservableCollection<RentalDisplayRecord> _recentRentals = new();

        public ObservableCollection<string> TimePeriodOptions { get; } = new()
            { "Last 7 Days", "Last 30 Days", "This Week", "This Month" };

        [ObservableProperty]
        private string _selectedTimePeriod = "Last 7 Days"; // Default selection

        [ObservableProperty]
        private bool _isLoading;

        // Properties for binding in XAML Financial Summary (derived from FinancialStats)
        public string TurnoverAmount => FinancialStats.Turnover.CurrentPeriodAmount;
        public string TurnoverChange => FinancialStats.Turnover.PercentageChange;
        public string TurnoverChangeDescription => FinancialStats.Turnover.ChangeDescription;

        public string IncomeAmount => FinancialStats.Income.CurrentPeriodAmount;
        public string IncomeChange => FinancialStats.Income.PercentageChange;
        public string IncomeChangeDescription => FinancialStats.Income.ChangeDescription;

        public string OutflowAmount => FinancialStats.Outflow.CurrentPeriodAmount;
        public string OutflowChange => FinancialStats.Outflow.PercentageChange;
        public string OutflowChangeDescription => FinancialStats.Outflow.ChangeDescription;


        public DashboardViewModel(
            App.NavigationServiceFactory navigationServiceFactory,
            IDashboardService dashboardService)
        {
            _navigationServiceFactory = navigationServiceFactory;
            _dashboardService = dashboardService;

            _ = LoadDashboardDataAsync();
        }

        partial void OnSelectedTimePeriodChanged(string value)
        {
            _ = LoadRecentRentalsAsync(); // Reload recent rentals when period changes
        }

        private async Task LoadDashboardDataAsync()
        {
            IsLoading = true;
            try
            {
                // Parallel loading for independent parts
                var financialTask = _dashboardService.GetFinancialSummaryAsync();
                var fleetTask = _dashboardService.GetFleetSummaryAsync(); // Uses default maxCars = 8
                var rentalsTask = LoadRecentRentalsAsync(); // Initial load with default period
                var financialChartDataTask = _dashboardService.GetFinancialChartDataAsync(); // NEW
                var rentalChartDataTask = _dashboardService.GetRentalChartDataAsync();       // NEW

                await Task.WhenAll(financialTask, fleetTask, rentalsTask, financialChartDataTask, rentalChartDataTask);

                FinancialStats = financialTask.Result ?? new DashboardStats(); // Ensure not null
                OnPropertyChanged(nameof(TurnoverAmount)); // Notify derived properties
                OnPropertyChanged(nameof(TurnoverChange));
                OnPropertyChanged(nameof(TurnoverChangeDescription));
                OnPropertyChanged(nameof(IncomeAmount));
                OnPropertyChanged(nameof(IncomeChange));
                OnPropertyChanged(nameof(IncomeChangeDescription));
                OnPropertyChanged(nameof(OutflowAmount));
                OnPropertyChanged(nameof(OutflowChange));
                OnPropertyChanged(nameof(OutflowChangeDescription));


                FleetSummary.Clear();
                if (fleetTask.Result != null)
                {
                    foreach (var car in fleetTask.Result) FleetSummary.Add(car);
                }

                // Process Financial Chart Data
                var financialData = financialChartDataTask.Result;
                if (financialData != null && financialData.Any())
                {
                    FinancialSeries = new ISeries[]
                    {
                        new LineSeries<double> // Or ColumnSeries, etc.
                        {
                            Name = "Income",
                            Values = financialData.Select(d => d.Income).ToArray(),
                            Fill = null,
                            GeometrySize = 5,
                            LineSmoothness = 0.5,
                            Stroke = new LinearGradientPaint(SKColors.LightCyan, SKColors.Green) { StrokeThickness = 3 },
                            GeometryStroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 3 }
                        },
                        new LineSeries<double>
                        {
                            Name = "Expenses",
                            Values = financialData.Select(d => d.Expenses).ToArray(),
                            Fill = null,
                            GeometrySize = 5,
                            LineSmoothness = 0.5,
                            Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 3 },
                            GeometryStroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 3 }
                        }
                    };
                    FinancialXAxes = new[] { new Axis { Name = "Month", Labels = financialData.Select(d => d.MonthYearLabel).ToArray(), LabelsRotation = 15, TextSize = 10 } };
                }
                else
                {
                    FinancialSeries = Array.Empty<ISeries>(); // Clear if no data
                    FinancialXAxes = new[] { new Axis { Name = "Month", LabelsRotation = 15 } };
                }


                // Process Monthly Rental Chart Data
                var rentalData = rentalChartDataTask.Result;
                if (rentalData != null && rentalData.Any())
                {
                    MonthlyRentalSeries = new ISeries[]
                    {
                        new ColumnSeries<int> // Or LineSeries
                        {
                            Name = "Rentals",
                            Values = rentalData.Select(d => d.RentalCount).ToArray(),
                            Fill = new SolidColorPaint(SKColor.Parse("#006D77")),
                            DataLabelsPaint = new SolidColorPaint(SKColors.White),
                            DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top,
                            DataLabelsFormatter = (point) => point.Coordinate.PrimaryValue.ToString("N0"),
                        }
                    };
                    MonthlyRentalXAxes = new[] { new Axis { Name = "Month", Labels = rentalData.Select(d => d.MonthYearLabel).ToArray(), LabelsRotation = 15, TextSize = 10 } };
                }
                else
                {
                    MonthlyRentalSeries = Array.Empty<ISeries>();
                    MonthlyRentalXAxes = new[] { new Axis { Name = "Month", LabelsRotation = 15 } };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard data: {ex.Message}", "Dashboard Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // Initialize with defaults on error
                FinancialStats = new DashboardStats();
                FleetSummary.Clear();
                RecentRentals.Clear();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadRecentRentalsAsync()
        {
            // This can be part of IsLoading or have its own mini-loader if desired
            // For simplicity, using the main IsLoading flag.
            IsLoading = true; // Or a specific IsRecentRentalsLoading
            try
            {
                var rentals = await _dashboardService.GetRecentRentalsAsync(SelectedTimePeriod);
                RecentRentals.Clear();
                if (rentals != null)
                {
                    // Take top N for display, e.g., top 5
                    foreach (var rental in rentals.Take(5)) // Show top 5 recent rentals
                    {
                        RecentRentals.Add(rental);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading recent rentals: {ex.Message}", "Rentals Error", MessageBoxButton.OK, MessageBoxImage.Error);
                RecentRentals.Clear();
            }
            finally
            {
                IsLoading = false; // Reset main flag after all parts or the specific flag
            }
        }


        // --- Sidebar Navigation Commands ---
        [RelayCommand] private void NavigateToDashboard() { /* Already on Dashboard, maybe refresh? */ _ = LoadDashboardDataAsync(); }
        [RelayCommand] private void NavigateToRentals() => _navigationServiceFactory.Create<RentalsViewModel>().Navigate();
        [RelayCommand] private void NavigateToCustomers() => _navigationServiceFactory.Create<CustomersViewModel>().Navigate();
        [RelayCommand] private void NavigateToPayments() => _navigationServiceFactory.Create<PaymentsViewModel>().Navigate();
        [RelayCommand] private void NavigateToExpenses() => _navigationServiceFactory.Create<ExpensesViewModel>().Navigate();
        [RelayCommand] private void NavigateToFinances() => _navigationServiceFactory.Create<FinancesViewModel>().Navigate(); // Assuming FinancesView exists
        [RelayCommand] private void NavigateToCars() => _navigationServiceFactory.Create<CarsViewModel>().Navigate();
        [RelayCommand] private void Logout() => _navigationServiceFactory.Create<LoginViewModel>().Navigate();
    }

    public class MonthlyFinancialChartData
    {
        public string MonthYearLabel { get; set; } = string.Empty; // e.g., "Jan 23"
        public double Income { get; set; }
        public double Expenses { get; set; }
    }

    public class MonthlyRentalCountChartData
    {
        public string MonthYearLabel { get; set; } = string.Empty; // e.g., "Jan 23"
        public int RentalCount { get; set; }
    }
}