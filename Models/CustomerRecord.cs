// NoorRAC/Models/CustomerRecord.cs
using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NoorRAC.Models
{
    public partial class CustomerRecord : ObservableObject, IEquatable<CustomerRecord>
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

        public override string ToString()
        {
            return Name ?? string.Empty;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as CustomerRecord);
        }

        public bool Equals(CustomerRecord? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            // If ID is 0, it's a new, unsaved record, rely on reference equality or other fields.
            if (this.ID == 0 && other.ID == 0) return ReferenceEquals(this, other);
            return this.ID == other.ID; // Compare by unique ID
        }

        public override int GetHashCode()
        {
            // If ID is 0, use base.GetHashCode() to avoid multiple new objects having hash code 0.
            if (ID == 0) return base.GetHashCode();
            return ID.GetHashCode();
        }

        public static bool operator ==(CustomerRecord? left, CustomerRecord? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(CustomerRecord? left, CustomerRecord? right)
        {
            return !Equals(left, right);
        }
    }
}