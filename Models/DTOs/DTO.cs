// --- In ViewModels/ or Models/DTOs/ ---

using CommunityToolkit.Mvvm.ComponentModel;
using System;

// Used for Fleet Summary lists
public partial class CarSummaryViewModel : ObservableObject
{
    [ObservableProperty] private string? _makeModel; // e.g., "Toyota Corolla"
    [ObservableProperty] private string? _regNumber;
}

// Used for Fleet Summary Pending Returns list
public partial class PendingReturnViewModel : ObservableObject
{
    [ObservableProperty] private string? _makeModel;
    [ObservableProperty] private DateTime? _dueDate;
}

// Used for the Recent Rentals DataGrid
public partial class RentalRecordViewModel : ObservableObject
{
    [ObservableProperty] private int _id;
    [ObservableProperty] private string? _clientName;
    [ObservableProperty] private string? _carType; // e.g., "Toyota Corolla"
    [ObservableProperty] private string? _carNumber;
    [ObservableProperty] private string? _status; // e.g., "Active", "Completed", "Upcoming"
}

// Used for the Financial Summary cards
public class FinanceSummary
{
    public string? TurnoverAmountFormatted { get; set; }
    public string? TurnoverChangeFormatted { get; set; }
    public string? TurnoverChangeDescription { get; set; }
    public string? IncomeAmountFormatted { get; set; }
    public string? IncomeChangeFormatted { get; set; }
    public string? IncomeChangeDescription { get; set; }
    public string? OutflowAmountFormatted { get; set; }
    public string? OutflowChangeFormatted { get; set; }
    public string? OutflowChangeDescription { get; set; }
}