// NoorRAC/App.xaml.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NoorRAC.Services; // Your custom services namespace
using NoorRAC.Stores;
using NoorRAC.ViewModels; // Add using for ViewModels namespace
using System;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel; // Base class for ViewModels
using Microsoft.Extensions.Configuration;

namespace NoorRAC
{
    public partial class App : Application
    {
        private readonly IHost _host;

        // Define the delegate type for your factory if LoginViewModel and others use it directly
        // This matches the type used in LoginViewModel's constructor: App.NavigationServiceFactory
        // If it's truly meant to be `App.NavigationServiceFactory`, then the class below
        // should be nested or named `App.NavigationServiceFactory`.
        // For clarity, I'm keeping it as a top-level `NavigationServiceFactory` class here,
        // and assuming consumers will inject `NavigationServiceFactory` directly.
        // If you need the exact type `App.NavigationServiceFactory`, you'd do:
        // public partial class App { public class NavigationServiceFactory { /* ... */ } }
        // For now, let's assume it's just `NavigationServiceFactory`.

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, builder) =>
                {
                    // Add other configuration sources if needed, e.g., appsettings.json
                    // builder.SetBasePath(context.HostingEnvironment.ContentRootPath);
                    // builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    // context.Configuration is available here if you added config sources above
                    ConfigureServices(services, context.Configuration);
                })
                .Build();
        }

        private void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Stores (Singletons are appropriate here)
            services.AddSingleton<NavigationStore>();

            // Services
            // The Func<Type, ObservableObject> is a good way to create VM instances via DI
            services.AddSingleton<Func<Type, ObservableObject>>(provider => viewModelType =>
                (ObservableObject)provider.GetRequiredService(viewModelType));

            // Register the modified NavigationServiceFactory
            services.AddSingleton<NavigationServiceFactory>(); // Implementation updated below

            // Database / Authentication Services (Register Interfaces and Implementations)
            // Example: services.AddSingleton<IConfiguration>(configuration); // If needed by services
            services.AddSingleton<IAuthenticationService, MySqlAuthenticationService>();
            // services.AddSingleton<IDataService, MySqlDataService>(provider =>
            //     new MySqlDataService(provider.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection"))
            // );

            // Assuming your service implementations exist:
            // services.AddSingleton<Services.ICarService, MySqlCarService>();
             services.AddSingleton<Services.IRentalService, MySQLRentalService>();
            // services.AddSingleton<Services.IFinanceService, MySqlFinanceService>();
            services.AddSingleton<Services.ICustomerService, MySQLCustomerService>(); // Register customer service


            // ViewModels (Typically Transient)
            // Transient: New instance each time requested
            services.AddTransient<LoginViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<RentalsViewModel>();

            services.AddTransient<CustomersViewModel>();
            services.AddTransient<AddNewCustomerViewModel>();
            services.AddTransient<EditCustomerViewModel>(); // Register EditCustomerViewModel

            // Register other ViewModels as needed
            // services.AddTransient<PaymentsViewModel>();
            // services.AddTransient<ExpensesViewModel>();
            // services.AddTransient<FinancesViewModel>();
            // services.AddTransient<CarsViewModel>();


            // MainViewModel (Singleton as it represents the main window's lifetime)
            services.AddSingleton<MainViewModel>();

            // MainWindow (Register the Window itself)
            services.AddSingleton(provider => new MainWindow()
            {
                DataContext = provider.GetRequiredService<MainViewModel>()
            });
        }

        // Updated NavigationServiceFactory (Place this inside App.xaml.cs or its own file)
        public class NavigationServiceFactory
        {
            private readonly NavigationStore _navigationStore;
            private readonly Func<Type, ObservableObject> _viewModelFactory; // For creating NEW instances
            private readonly IServiceProvider _serviceProvider; // To pass to custom instance factories

            public NavigationServiceFactory(
                NavigationStore navigationStore,
                Func<Type, ObservableObject> viewModelFactory,
                IServiceProvider serviceProvider) // Inject IServiceProvider
            {
                _navigationStore = navigationStore;
                _viewModelFactory = viewModelFactory;
                _serviceProvider = serviceProvider;
            }

            /// <summary>
            /// Creates a navigation service.
            /// </summary>
            /// <typeparam name="TViewModel">The type of ViewModel to navigate to.</typeparam>
            /// <param name="existingViewModelInstanceFactory">
            /// Optional factory function that returns an already existing/configured instance of TViewModel.
            /// If provided, this instance will be used for navigation.
            /// The IServiceProvider is passed to allow resolving dependencies if the factory needs them,
            /// though often it will just return a captured instance.
            /// </param>
            /// <returns>An INavigationService for the specified ViewModel type.</returns>
            public INavigationService<TViewModel> Create<TViewModel>(
                Func<IServiceProvider, TViewModel>? existingViewModelInstanceFactory = null)
                where TViewModel : ObservableObject
            {
                Func<TViewModel> finalViewModelProvider;

                if (existingViewModelInstanceFactory != null)
                {
                    // Use the provided factory that returns an existing, configured instance.
                    // The IServiceProvider argument allows the lambda to access services if needed,
                    // but in our case for EditCustomerViewModel, it will just return the captured 'editCustomerVM'.
                    finalViewModelProvider = () => existingViewModelInstanceFactory(_serviceProvider);
                }
                else
                {
                    // Default behavior: create a new ViewModel instance using the registered factory.
                    finalViewModelProvider = () => (TViewModel)_viewModelFactory(typeof(TViewModel));
                }

                return new NavigationService<TViewModel>(_navigationStore, finalViewModelProvider);
            }
        }


        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>(); // Get MainWindow instance

            // Initial Navigation Logic
            var navigationServiceFactory = _host.Services.GetRequiredService<NavigationServiceFactory>();

            // Navigate to LoginViewModel (or Splash if you have one and it's different)
            // Your current OnStartup already navigates to Login twice after a delay.
            // Let's simplify: Navigate to Login once.
            // If you had a SplashViewModel, it would be:
            // var splashNav = navigationServiceFactory.Create<SplashViewModel>();
            // splashNav.Navigate();
            // mainWindow.Show(); // Show window after initial content is set
            // await Task.Delay(3000); // Splash screen duration
            // var loginNav = navigationServiceFactory.Create<LoginViewModel>();
            // loginNav.Navigate();

            // Current simplified startup:
            var initialNavigationService = navigationServiceFactory.Create<LoginViewModel>();
            initialNavigationService.Navigate();

            mainWindow.Show(); // Show the main window

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await _host.StopAsync(); // Gracefully stop the host
            _host.Dispose();        // Dispose of the host and its services
            base.OnExit(e);
        }
    }
}