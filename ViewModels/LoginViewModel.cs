using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoorRAC.Models;
using NoorRAC.Services;
using System;
using System.Threading.Tasks;
using System.Windows; // For MessageBox (consider a dedicated UI feedback service)

namespace NoorRAC.ViewModels
{
    public partial class LoginViewModel : ObservableObject // Use [ObservableProperty] attribute
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly App.NavigationServiceFactory _navigationServiceFactory; // Get factory for navigation

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))] // Re-evaluate CanExecute when Username changes
        private string? _username;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))] // Re-evaluate CanExecute when Password changes
        private string? _password;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private bool _isLoggingIn;

        // Constructor Injection
        public LoginViewModel(
            IAuthenticationService authenticationService,
            App.NavigationServiceFactory navigationServiceFactory)
        {
            _authenticationService = authenticationService;
            _navigationServiceFactory = navigationServiceFactory;
        }

        // AsyncRelayCommand for database operations
        [RelayCommand(CanExecute = nameof(CanLogin))]
        private async Task LoginAsync()
        {
            IsLoggingIn = true;
            ErrorMessage = null; // Clear previous errors

            try
            {
                User? loggedInUser = await _authenticationService.LoginAsync(Username ?? "", Password ?? "");

                if (loggedInUser != null)
                {
                    // Login Successful!

                    // Optional: Store logged-in user info (e.g., in a UserStore)

                    // --- Navigation Sequence ---
                    // 1. Navigate back to Splash (Fade In)
                    //var splashNav = _navigationServiceFactory.Create<SplashViewModel>();
                    //splashNav.Navigate();
                    // Small delay for UI update if needed

                    // (Implement fade-in animation for Splash)

                    // 3. Navigate to Dashboard (Fade out Splash, Fade in Dashboard)
                    // (Implement fade-out animation for Splash)
                    // var dashboardNav = _navigationServiceFactory.Create<DashboardViewModel>();
                    // dashboardNav.Navigate();
                    // 3. Navigate to Rentals (Temporary change from Dashboard)
                    var rentalsNav = _navigationServiceFactory.Create<RentalsViewModel>(); // Changed from DashboardViewModel
                    rentalsNav.Navigate();
                    // (Implement fade-in animation for Dashboard)

                }
                else
                {
                    // Login Failed
                    ErrorMessage = "Invalid username or password.";
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., database connection issues)
                ErrorMessage = $"An error occurred: {ex.Message}";
                // Log the full exception details
            }
            finally
            {
                IsLoggingIn = false;
            }
        }

        private bool CanLogin()
        {
            // Can only login if not already logging in and fields are not empty
            return !IsLoggingIn &&
                   !string.IsNullOrWhiteSpace(Username) &&
                   !string.IsNullOrWhiteSpace(Password);
        }
    }
}