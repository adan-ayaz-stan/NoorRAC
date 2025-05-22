// NoorRAC/Models/PaymentDisplayRecord.cs
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace NoorRAC.Models
{
    public partial class PaymentDisplayRecord : ObservableObject
    {
        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private string? _customerName;

        [ObservableProperty]
        private decimal _amountPaid;

        [ObservableProperty]
        private decimal _dueAmountOnRental; // Total due of the associated rental

        [ObservableProperty]
        private string? _paymentMethod;

        [ObservableProperty]
        private DateTime _paymentDate;

        [ObservableProperty]
        private string? _rentalInfo; // e.g., "Rental #101 - Toyota Corolla"

        public string PaymentDateString => PaymentDate.ToString("yyyy-MM-dd");
        public string AmountPaidString => $"PKR {AmountPaid:N0}";
        public string DueAmountOnRentalString => $"PKR {DueAmountOnRental:N0}";
    }
}