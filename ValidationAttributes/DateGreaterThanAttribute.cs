// NoorRAC/ValidationAttributes/DateGreaterThanAttribute.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection; // For GetProperty and DisplayAttribute

namespace NoorRAC.ValidationAttributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class DateGreaterThanAttribute : ValidationAttribute
    {
        private readonly string _comparisonProperty;
        private object? _validationContextObjectInstance; // To store for FormatErrorMessage

        public DateGreaterThanAttribute(string comparisonProperty, string? errorMessage = null)
        {
            _comparisonProperty = comparisonProperty;
            // Set a default error message if none is provided, allowing for dynamic property names
            ErrorMessage = errorMessage ?? "{0} must be after {1}.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            _validationContextObjectInstance = validationContext.ObjectInstance; // Store for FormatErrorMessage

            var currentValue = (DateTime?)value;

            PropertyInfo? property = validationContext.ObjectType.GetProperty(_comparisonProperty);
            if (property == null)
            {
                // This indicates a programming error (misspelled comparisonProperty name)
                return new ValidationResult($"Unknown property: {_comparisonProperty}");
            }

            var comparisonValue = (DateTime?)property.GetValue(validationContext.ObjectInstance);

            // Only proceed if both dates have values
            if (currentValue.HasValue && comparisonValue.HasValue)
            {
                // Compare only the Date part, ignoring time, if that's the business rule
                if (currentValue.Value.Date <= comparisonValue.Value.Date)
                {
                    // Use validationContext.MemberName to ensure the error is associated with the correct property
                    // It will be null if MemberName is not set, which shouldn't happen for property validation.
                    return new ValidationResult(FormatErrorMessage(validationContext.DisplayName), new[] { validationContext.MemberName! });
                }
            }
            // If one or both dates are null, and the property being validated is also nullable DateTime,
            // this might be considered valid depending on requirements.
            // If the property is non-nullable DateTime, [Required] should handle null.
            // For this specific attribute, if currentValue is null, standard [Required] handles it.
            // If comparisonValue is null, the logic might need adjustment based on how that should be treated.
            // Assuming here that if comparisonValue is null, the "greater than" condition cannot be meaningfully checked,
            // so it might be considered valid by this specific attribute, letting other attributes handle nullability.

            return ValidationResult.Success;
        }

        public override string FormatErrorMessage(string name)
        {
            // Attempt to get the DisplayName of the comparison property for a friendlier error message
            string comparisonDisplayName = _comparisonProperty; // Default to property name
            if (_validationContextObjectInstance != null)
            {
                var comparisonPropertyInfo = _validationContextObjectInstance.GetType().GetProperty(_comparisonProperty);
                if (comparisonPropertyInfo != null)
                {
                    var displayAttribute = comparisonPropertyInfo.GetCustomAttribute<DisplayAttribute>();
                    if (displayAttribute != null && !string.IsNullOrEmpty(displayAttribute.GetName()))
                    {
                        comparisonDisplayName = displayAttribute.GetName()!;
                    }
                }
            }
            return string.Format(ErrorMessageString, name, comparisonDisplayName);
        }
    }
}