// NoorRAC/Models/PaymentRecord.cs
using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NoorRAC.Models
{
    public partial class PaymentRecord : ObservableObject
    {
        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private int _customerId;

        [ObservableProperty]
        private int? _rentalId; // Nullable if payment is not tied to a specific rental

        [ObservableProperty]
        private decimal _amountPaid; // Keep as decimal for C# logic

        [ObservableProperty]
        private string? _paymentMethod; // 'cash','card','online','other'

        [ObservableProperty]
        private DateTime _paymentDate = DateTime.Today;

        [ObservableProperty]
        private string? _comment;
    }
}