// NoorRAC/Models/Car.cs
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NoorRAC.Models
{
    public partial class Car : ObservableObject, IEquatable<Car> 
    {
        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private string _registrationNumber = string.Empty;

        [ObservableProperty]
        private string _carModel = string.Empty;

        [ObservableProperty]
        private string _carMake = string.Empty;

        [ObservableProperty]
        private int _rentPerDay; // Storing as int as per DB, consider decimal if cents are involved

        [ObservableProperty]
        private string _ownerName = string.Empty;

        [ObservableProperty]
        private string _ownerCNIC = string.Empty;

        [ObservableProperty]
        private string _ownerPhone = string.Empty;

        [ObservableProperty]
        private int _ownerLendingFees; // Storing as int, consider decimal

        // For ComboBox display in other ViewModels if needed
        public string DisplayNameWithRegNo => $"{RegistrationNumber} - {CarMake} {CarModel}";

        public override string ToString()
        {
            return DisplayNameWithRegNo ?? string.Empty;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Car);
        }

        public bool Equals(Car? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            // If ID is 0, it's a new, unsaved record, rely on reference equality or other fields.
            if (this.Id == 0 && other.Id == 0) return ReferenceEquals(this, other);
            return this.Id == other.Id; // Compare by unique ID
        }

        public override int GetHashCode()
        {
            // If ID is 0, use base.GetHashCode() to avoid multiple new objects having hash code 0.
            if (Id == 0) return base.GetHashCode();
            return Id.GetHashCode();
        }

        public static bool operator ==(Car? left, Car? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Car? left, Car? right)
        {
            return !Equals(left, right);
        }
    }
}