// NoorRAC/Models/CustomerRecord.cs
using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NoorRAC.Models
{
    public partial class CustomerRecord : ObservableObject
    {
        [ObservableProperty]
        private int _iD;

        [ObservableProperty]
        private string? _name;

        [ObservableProperty]
        private string? _cNIC; // National Identity Card Number

        [ObservableProperty]
        private string? _contactInfo; // Could be phone, email, etc.

        [ObservableProperty]
        private int _totalRentals;

        [ObservableProperty]
        private string? _address;

        [ObservableProperty]
        private decimal _outstandingPayments; // Use decimal for currency

        [ObservableProperty]
        private DateTime _dateJoined;

        // For display purposes in DataGrid if specific formatting is needed
        public string OutstandingPaymentsString => OutstandingPayments.ToString("C"); // Format as currency
        public string DateJoinedString => DateJoined.ToString("yyyy-MM-dd");
    }
}