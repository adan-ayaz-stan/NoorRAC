// NoorRAC/ViewModels/FinancesViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Win32; // For SaveFileDialog
using NoorRAC.Models;
using NoorRAC.Services;
using QuestPDF.Helpers;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace NoorRAC.ViewModels
{
    public partial class FinancesViewModel : ObservableObject
    {
        private readonly App.NavigationServiceFactory _navigationServiceFactory;
        private readonly IFinanceService _financeService;
        private readonly IPdfGenerationService _pdfGenerationService;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private DateTime _fromDate = DateTime.Today.AddMonths(-1); // Default to last month

        [ObservableProperty]
        private DateTime _toDate = DateTime.Today;

        [ObservableProperty]
        private FinancialOverviewStats? _overviewStats;

        public ObservableCollection<ISeries> Series { get; set; } = new();
        public ObservableCollection<Axis> XAxes { get; set; } = new();
        public ObservableCollection<Axis> YAxes { get; set; } = new();


        [ObservableProperty]
        private ObservableCollection<FinancialTransactionRecord> _financeRecords = new();

        [ObservableProperty]
        private string? _searchTerm;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _pageSize = 10;

        [ObservableProperty]
        private int _totalItems;

        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

        // Properties for the XAML hardcoded values (can be bound if dynamic)
        public decimal TotalIncomeForDisplay => OverviewStats?.TotalIncomeForPeriod ?? 0;
        public decimal ReceivedPaymentsForDisplay => OverviewStats?.TotalIncomeForPeriod ?? 0; // Assuming this is what "Received Payments" card shows
        public string DuePaymentsForDisplay => "Rs. 56,500"; // Placeholder - Fetch this from rental service if needed


        public FinancesViewModel(
            App.NavigationServiceFactory navigationServiceFactory,
            IFinanceService financeService,
            IPdfGenerationService pdfGenerationService,
            IServiceProvider serviceProvider)
        {
            _navigationServiceFactory = navigationServiceFactory;
            _financeService = financeService;
            _pdfGenerationService = pdfGenerationService;
            _serviceProvider = serviceProvider;

            InitializeChart();
            _ = LoadAllFinancialDataAsync();
        }

        private void InitializeChart()
        {
            Series = new ObservableCollection<ISeries>
            {
                new LineSeries<DailyFinancialSummary>
                {
                    Name = "Payments",
                    Values = new ObservableCollection<DailyFinancialSummary>(),
                    Mapping = (summary, index) => new LiveChartsCore.Defaults.Coordinates(index, (double)summary.TotalPayments),
                    Fill = null,
                    GeometrySize = 10,
                    LineSmoothness = 0.5,
                     Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 2 },
                    GeometryStroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 2 }
                },
                new LineSeries<DailyFinancialSummary>
                {
                    Name = "Expenses",
                    Values = new ObservableCollection<DailyFinancialSummary>(),
                    Mapping = (summary, index) => new LiveChartsCore.Defaults.Coordinates(index, (double)summary.TotalExpenses),
                    Fill = null,
                    GeometrySize = 10,
                    LineSmoothness = 0.5,
                    Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 2 },
                    GeometryStroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 2 }
                }
            };

            XAxes = new ObservableCollection<Axis>
            {
                new Axis
                {
                    Name = "Date",
                    LabelsRotation = 15,
                    Labeler = value =>_dailySummariesForChart.Count > value && value >=0 ? _dailySummariesForChart[(int)value].Date.ToString("MMM dd") : "",
                    UnitWidth = 1,
                    MinStep = 1,
                }
            };
            YAxes = new ObservableCollection<Axis>
            {
                new Axis
                {
                    Name = "Amount (Rs.)",
                    Labeler = value => value.ToString("N0", CultureInfo.InvariantCulture), // Format as number
                    MinLimit = 0 // Ensure Y-axis starts at 0
                }
            };
        }

        private List<DailyFinancialSummary> _dailySummariesForChart = new(); // To help with X-axis labels

        partial void OnFromDateChanged(DateTime value) => _ = DateRangeChangedAsync();
        partial void OnToDateChanged(DateTime value) => _ = DateRangeChangedAsync();

        private async Task DateRangeChangedAsync()
        {
            CurrentPage = 1; // Reset pagination
            await LoadAllFinancialDataAsync();
        }

        [RelayCommand]
        private async Task SetDateRange(string range)
        {
            DateTime today = DateTime.Today;
            switch (range.ToLower())
            {
                case "today":
                    FromDate = today;
                    ToDate = today;
                    break;
                case "thisweek":
                    FromDate = today.AddDays(-(int)today.DayOfWeek); // Assuming Sunday is start of week
                    if (CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek == DayOfWeek.Monday)
                    {
                        FromDate = today.AddDays(DayOfWeek.Monday - today.DayOfWeek);
                        if (FromDate > today) FromDate = FromDate.AddDays(-7); // handles if today is Sunday and week starts Monday
                    }
                    ToDate = FromDate.AddDays(6);
                    break;
                case "thismonth":
                    FromDate = new DateTime(today.Year, today.Month, 1);
                    ToDate = FromDate.AddMonths(1).AddDays(-1);
                    break;
                case "thisyear":
                    FromDate = new DateTime(today.Year, 1, 1);
                    ToDate = new DateTime(today.Year, 12, 31);
                    break;
            }
            // The OnFromDateChanged/OnToDateChanged handlers will trigger LoadAllFinancialDataAsync
        }


        private async Task LoadAllFinancialDataAsync()
        {
            IsLoading = true;
            try
            {
                // Load Overview Stats
                OverviewStats = await _financeService.GetFinancialOverviewStatsAsync(FromDate, ToDate);
                OnPropertyChanged(nameof(TotalIncomeForDisplay));
                OnPropertyChanged(nameof(ReceivedPaymentsForDisplay));
                // OnPropertyChanged(nameof(DuePaymentsForDisplay)); // If this becomes dynamic

                // Load Chart Data
                _dailySummariesForChart = await _financeService.GetDailySummariesAsync(FromDate, ToDate);
                if (Series.Count >= 2) // Ensure series are initialized
                {
                    ((LineSeries<DailyFinancialSummary>)Series[0]).Values = new ObservableCollection<DailyFinancialSummary>(_dailySummariesForChart);
                    ((LineSeries<DailyFinancialSummary>)Series[1]).Values = new ObservableCollection<DailyFinancialSummary>(_dailySummariesForChart);
                }
                // Update X-axis labels based on new data
                if (XAxes.Any()) XAxes[0].Name = $"Date ({_dailySummariesForChart.Count} days)"; // Update axis name or re-evaluate labeler


                // Load Table Data
                string? currentSearchTerm = string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm;
                TotalItems = await _financeService.GetTotalCombinedTransactionsCountAsync(FromDate, ToDate, currentSearchTerm);
                var transactions = await _financeService.GetCombinedTransactionsAsync(FromDate, ToDate, CurrentPage, PageSize, currentSearchTerm);

                FinanceRecords.Clear();
                if (transactions != null)
                {
                    foreach (var tx in transactions) FinanceRecords.Add(tx);
                }
                OnPropertyChanged(nameof(TotalPages));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading financial data: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            CurrentPage = 1;
            await LoadAllFinancialDataAsync(); // Re-load all data as search might affect chart/overview too if it were more granular
        }


        [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
        private async Task PreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadTableDataOnlyAsync(); // Only reload table for pagination
            }
        }
        private bool CanGoToPreviousPage() => CurrentPage > 1 && !IsLoading;

        [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
        private async Task NextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadTableDataOnlyAsync(); // Only reload table for pagination
            }
        }
        private bool CanGoToNextPage() => CurrentPage < TotalPages && !IsLoading;

        // Helper to only reload table data for pagination or search without full refresh
        private async Task LoadTableDataOnlyAsync()
        {
            IsLoading = true;
            try
            {
                string? currentSearchTerm = string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm;
                // TotalItems should already be set by LoadAllFinancialDataAsync or a dedicated call if search changes it
                var transactions = await _financeService.GetCombinedTransactionsAsync(FromDate, ToDate, CurrentPage, PageSize, currentSearchTerm);

                FinanceRecords.Clear();
                if (transactions != null)
                {
                    foreach (var tx in transactions) FinanceRecords.Add(tx);
                }
                OnPropertyChanged(nameof(TotalPages)); // Ensure TotalPages is up-to-date if TotalItems changed
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading table data: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SeeDetailAsync(FinancialTransactionRecord? record)
        {
            if (record == null) return;

            if (record.SourceTable == "Payment")
            {
                // Navigate to EditPaymentView or PaymentDetailView
                // var editPaymentVM = _serviceProvider.GetRequiredService<EditPaymentViewModel>();
                // await editPaymentVM.LoadPaymentDataAsync(record.OriginalId);
                // _navigationServiceFactory.Create<EditPaymentViewModel>(sp => editPaymentVM).Navigate();
                MessageBox.Show($"View Payment Detail for ID: {record.OriginalId}", "Info");
            }
            else if (record.SourceTable == "Expense")
            {
                // Navigate to EditExpenseView or ExpenseDetailView
                // var editExpenseVM = _serviceProvider.GetRequiredService<EditExpenseViewModel>();
                // await editExpenseVM.LoadExpenseAsync(record.OriginalId); // Assuming EditExpenseVM has such a method
                // _navigationServiceFactory.Create<EditExpenseViewModel>(sp => editExpenseVM).Navigate();
                MessageBox.Show($"View Expense Detail for ID: {record.OriginalId}", "Info");
            }
            await Task.CompletedTask;
        }

        [RelayCommand]
        private async Task PrintToPdfAsync()
        {
            // 1. Ask user for timespan (using a simple dialog or a custom one)
            // For simplicity, I'll make a simple input dialog here.
            // In a real app, you'd use a proper dialog service or a custom input window.
            string? selectedTimespanOption = await ShowTimespanSelectionDialogAsync();
            if (string.IsNullOrEmpty(selectedTimespanOption)) return;

            DateTime pdfFromDate = FromDate; // Default to current view range
            DateTime pdfToDate = ToDate;
            DateTime today = DateTime.Today;

            switch (selectedTimespanOption.ToLower())
            {
                case "today": pdfFromDate = today; pdfToDate = today; break;
                case "this week":
                    pdfFromDate = today.AddDays(-(int)today.DayOfWeek); // Adjust for culture if needed
                    if (CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek == DayOfWeek.Monday)
                    {
                        pdfFromDate = today.AddDays(DayOfWeek.Monday - today.DayOfWeek);
                        if (pdfFromDate > today) pdfFromDate = pdfFromDate.AddDays(-7);
                    }
                    pdfToDate = pdfFromDate.AddDays(6);
                    break;
                case "this month": pdfFromDate = new DateTime(today.Year, today.Month, 1); pdfToDate = pdfFromDate.AddMonths(1).AddDays(-1); break;
                case "last six months": pdfFromDate = today.AddMonths(-6).AddDays(1 - today.Day); pdfToDate = today; break; // Approx last 6 full months up to today
                default: // Use current view's range if "Custom" or unrecognized
                    pdfFromDate = FromDate;
                    pdfToDate = ToDate;
                    break;
            }

            // 2. Show SaveFileDialog
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "PDF Document (*.pdf)|*.pdf",
                FileName = $"FinancialReport_{pdfFromDate:yyyyMMdd}_to_{pdfToDate:yyyyMMdd}.pdf",
                Title = "Save Financial Report"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                IsLoading = true;
                try
                {
                    // 3. Fetch all necessary data for the PDF for the selected range
                    var reportOverviewStats = await _financeService.GetFinancialOverviewStatsAsync(pdfFromDate, pdfToDate);
                    var reportTransactions = await _financeService.GetAllTransactionsForReportAsync(pdfFromDate, pdfToDate);
                    var reportDailySummaries = await _financeService.GetDailySummariesAsync(pdfFromDate, pdfToDate);


                    // 4. Generate PDF
                    await _pdfGenerationService.GenerateFinancialReportAsync(
                        saveFileDialog.FileName,
                        pdfFromDate,
                        pdfToDate,
                        reportOverviewStats,
                        reportTransactions,
                        reportDailySummaries);

                    MessageBox.Show($"Report saved to {saveFileDialog.FileName}", "PDF Generated", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error generating PDF: {ex.Message}", "PDF Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        // Simple dialog mock - replace with a proper dialog in a real app
        private Task<string?> ShowTimespanSelectionDialogAsync()
        {
            // This is a placeholder. You would create a custom dialog window.
            // For this example, I'm using a common pattern of a quick input box,
            // but a ComboBox or RadioButtons in a dialog would be better.
            var dialog = new Window { Title = "Select Report Timespan", Width = 300, Height = 180, WindowStartupLocation = WindowStartupLocation.CenterScreen };
            var stackPanel = new System.Windows.Controls.StackPanel { Margin = new Thickness(10) };
            var comboBox = new System.Windows.Controls.ComboBox { Margin = new Thickness(5) };
            comboBox.Items.Add("Current View Range");
            comboBox.Items.Add("Today");
            comboBox.Items.Add("This Week");
            comboBox.Items.Add("This Month");
            comboBox.Items.Add("Last Six Months");
            comboBox.SelectedIndex = 0;
            var okButton = new System.Windows.Controls.Button { Content = "OK", Margin = new Thickness(5), IsDefault = true };
            var cancelButton = new System.Windows.Controls.Button { Content = "Cancel", Margin = new Thickness(5), IsCancel = true };

            string? result = null;
            okButton.Click += (s, e) => { result = comboBox.SelectedItem as string; dialog.DialogResult = true; dialog.Close(); };
            cancelButton.Click += (s, e) => { dialog.DialogResult = false; dialog.Close(); };

            stackPanel.Children.Add(new System.Windows.Controls.TextBlock { Text = "Select data range for PDF:", Margin = new Thickness(5) });
            stackPanel.Children.Add(comboBox);
            var buttonPanel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            stackPanel.Children.Add(buttonPanel);
            dialog.Content = stackPanel;

            dialog.ShowDialog();
            return Task.FromResult(result);
        }


        // --- Sidebar Navigation Commands ---
        [RelayCommand] private void NavigateToDashboard() => _navigationServiceFactory.Create<DashboardViewModel>().Navigate();
        [RelayCommand] private void NavigateToRentals() => _navigationServiceFactory.Create<RentalsViewModel>().Navigate(); // XAML shows this active, correct if needed
        [RelayCommand] private void NavigateToCustomers() => _navigationServiceFactory.Create<CustomersViewModel>().Navigate();
        [RelayCommand] private void NavigateToPayments() => _navigationServiceFactory.Create<PaymentsViewModel>().Navigate();
        [RelayCommand] private void NavigateToExpenses() => _navigationServiceFactory.Create<ExpensesViewModel>().Navigate();
        [RelayCommand] private void NavigateToFinances() { /* Already here */ }
        [RelayCommand] private void NavigateToCars() => _navigationServiceFactory.Create<CarsViewModel>().Navigate();
        [RelayCommand] private void Logout() => _navigationServiceFactory.Create<LoginViewModel>().Navigate();
    }
}