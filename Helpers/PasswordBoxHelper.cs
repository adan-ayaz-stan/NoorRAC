using System.Windows;
using System.Windows.Controls;

namespace NoorRAC.Helpers // Adjust namespace if needed
{
    public static class PasswordBoxHelper
    {
        // Attached Dependency Property to hold the bound password
        public static readonly DependencyProperty BoundPasswordProperty =
            DependencyProperty.RegisterAttached(
                "BoundPassword",
                typeof(string),
                typeof(PasswordBoxHelper),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBoundPasswordChanged));
        // Note: Using BindsTwoWayByDefault simplifies the XAML binding slightly

        // Attached property getter
        public static string GetBoundPassword(DependencyObject d)
        {
            return (string)d.GetValue(BoundPasswordProperty);
        }

        // Attached property setter
        public static void SetBoundPassword(DependencyObject d, string value)
        {
            d.SetValue(BoundPasswordProperty, value);
        }


        // Internal attached property to prevent recursion during updates
        private static readonly DependencyProperty UpdatingPasswordProperty =
            DependencyProperty.RegisterAttached("UpdatingPassword", typeof(bool), typeof(PasswordBoxHelper), new PropertyMetadata(false));

        private static bool GetUpdatingPassword(DependencyObject dp)
        {
            return (bool)dp.GetValue(UpdatingPasswordProperty);
        }

        private static void SetUpdatingPassword(DependencyObject dp, bool value)
        {
            dp.SetValue(UpdatingPasswordProperty, value);
        }


        // --- Callbacks and Event Handlers ---

        private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBox passwordBox)
            {
                // Detach old handler if reusing the PasswordBox instance
                passwordBox.PasswordChanged -= HandlePasswordChanged;

                string newPassword = (string)e.NewValue;

                // Check if the password really changed - prevents executing the handler if the ViewModel value is set to the same value it already has.
                if (!GetUpdatingPassword(passwordBox) && passwordBox.Password != newPassword)
                {
                    passwordBox.Password = newPassword;
                }

                // Attach new handler
                passwordBox.PasswordChanged += HandlePasswordChanged;

                // Handle unloading to prevent memory leaks
                passwordBox.Unloaded -= PasswordBox_Unloaded; // Ensure only one handler
                passwordBox.Unloaded += PasswordBox_Unloaded;
            }
        }

        private static void HandlePasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                // Set a flag to indicate that we're updating the source from the control
                SetUpdatingPassword(passwordBox, true);
                // Update the BoundPassword attached property, which will update the ViewModel source
                SetBoundPassword(passwordBox, passwordBox.Password);
                // Reset the flag
                SetUpdatingPassword(passwordBox, false);
            }
        }

        private static void PasswordBox_Unloaded(object sender, RoutedEventArgs e)
        {
            // Clean up event handler when the control is unloaded
            if (sender is PasswordBox passwordBox)
            {
                passwordBox.PasswordChanged -= HandlePasswordChanged;
                passwordBox.Unloaded -= PasswordBox_Unloaded;
            }
        }
    }
}