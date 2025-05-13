NoorRAC is a car rental application specifically developed for Noor Tourism Pvt. Ltd. using C# and WPF. The project follows the MVVM architecture to ensure a clean separation of concerns and maintainability.

## Project Overview

NoorRAC aims to provide a seamless car rental experience for customers, allowing them to browse available cars, make reservations, and manage their bookings. The application is designed to be user-friendly and efficient, catering to the needs of both customers and administrators.

## Features

- Manage Rentals
- Manage Customers
- Manage Cars
- Manage Payments & Expenses
- Dashboard Overview
- Finances Overview

## Technologies Used

- .NET 8
- C#
- WPF (Windows Presentation Foundation)
- MVVM (Model-View-ViewModel) architecture

## Team Members

- Adan Ayaz (Project Manager, Lead Designer, Lead Developer)
- Syed Talal Hamdani (Designer, Developer)
- Yumnah Abdul Quddous (Developer)
- Fatima Liaqat (Developer)
- Abdullah (Developer)

## Project Structure

The project is structured based on the MVVM architecture, which includes the following components:

- **Model**: Represents the data and business logic of the application.
- **View**: Represents the UI components and defines the layout of the application.
- **ViewModel**: Acts as an intermediary between the Model and the View, handling user interactions and updating the View accordingly.

## Getting Started

To get started with the project, follow these steps:

1. Clone the repository:
2. Open the solution in Visual Studio.
3. Build the solution to restore the required packages.
4. Run the application.

## Contributing

We welcome contributions from the community. If you would like to contribute, please follow these steps:

1. Fork the repository.
2. Create a new branch for your feature or bugfix.
3. Commit your changes and push them to your forked repository.
4. Create a pull request with a detailed description of your changes.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for more details.

## Contact

For any inquiries or support, please contact Adan Ayaz at [adanayaztracer@gmail.com].

# Noor Rent a Car (NoorRAC) - Architecture Document

This document outlines the architectural patterns, conventions, and technologies used in the NoorRAC WPF application. Its purpose is to guide development and ensure consistency.

## 1. Core Architecture: MVVM (Model-View-ViewModel)

We use the Model-View-ViewModel (MVVM) pattern to separate concerns within the application:

*   **Model:** Represents the application's data entities and potentially business logic. These are simple C# classes (POCOs - Plain Old CLR Objects) located in the `Models` folder.
    *   *Example:* `Models/Car.cs`, `Models/Customer.cs`, `Models/User.cs`. They contain properties corresponding to database table columns.

*   **View:** Defines the structure, layout, and appearance of what the user sees (the UI). Views are written in XAML and reside in the `Views` folder. They should contain minimal to no code-behind. Views bind to properties and commands exposed by their corresponding ViewModel.
    *   *Example:* `Views/LoginScreen.xaml`, `Views/Dashboard.xaml`, `Views/Cars.xaml`.

*   **ViewModel:** Acts as the intermediary between the View and the Model. It handles the View's presentation logic and state. It retrieves data from services (which interact with Models/database), formats it for display, and exposes it to the View via data-bound properties. It also exposes Commands that the View binds to for handling user interactions (button clicks, etc.). ViewModels reside in the `ViewModels` folder and inherit from `CommunityToolkit.Mvvm.ComponentModel.ObservableObject` (or a custom `ViewModelBase`).
    *   *Example:* `ViewModels/LoginViewModel.cs`, `ViewModels/DashboardViewModel.cs`, `ViewModels/CarsViewModel.cs`.

**Key Benefits:** Testability (ViewModels can be tested without the UI), Maintainability, Separation of UI design from application logic.

## 2. Project Structure

The project follows a standard MVVM structure:

NoorRAC/
├── Models/ # Data entities (Car.cs, Customer.cs, etc.)
├── Views/ # UI definitions (XAML files like Cars.xaml, LoginScreen.xaml)
├── ViewModels/ # Logic for Views (CarsViewModel.cs, LoginViewModel.cs)
├── Services/ # Business logic, data access, navigation (IAuthenticationService.cs, MySqlDataService.cs, NavigationService.cs)
├── Stores/ # Shared application state (NavigationStore.cs, UserStore.cs)
├── Commands/ # ICommand implementations (if not using CommunityToolkit.Mvvm)
├── Resources/ # Images, Styles, Fonts, SVGs
├── Core/ # Optional: Base classes (ViewModelBase.cs if not using toolkit)
├── App.xaml # Application definition
├── App.xaml.cs # Application entry point, DI setup (.NET Host)
├── MainWindow.xaml # Main application window shell
├── MainWindow.xaml.cs# Code-behind for MainWindow (should be minimal)
├── NoorRAC.csproj # Project file
├── appsettings.json # Configuration (connection strings, etc.)
├── .gitignore # Git ignore rules
└── README.md # Project overview
└── ARCHITECTURE.md # This document

## 3. Dependency Injection and .NET Host

*   We utilize the **.NET Generic Host** (`Microsoft.Extensions.Hosting`) configured in `App.xaml.cs`.
*   **Benefits:**
    *   Centralized service configuration and lifetime management (Singleton, Scoped, Transient).
    *   Built-in support for logging and configuration (e.g., reading `appsettings.json`).
    *   Promotes loosely coupled design.
*   **Usage:** Services (like data access, navigation, authentication), ViewModels, and Stores are registered in the `ConfigureServices` method within `App.xaml.cs`. Dependencies are injected via constructors.
    *   *Example:* `LoginViewModel` receives `IAuthenticationService` and `NavigationServiceFactory` via its constructor.

## 4. Navigation

*   A **global Navigation Service** approach is used for switching between the main views/screens of the application (Login, Dashboard, Cars, Customers, etc.).
*   **Components:**
    *   `NavigationStore`: A singleton store (`Stores/NavigationStore.cs`) that holds a reference to the `CurrentViewModel` being displayed. It notifies subscribers when the `CurrentViewModel` changes.
    *   `INavigationService<TViewModel>`: An interface defining the contract for navigating *to* a specific ViewModel type.
    *   `NavigationService<TViewModel>`: The implementation (`Services/NavigationService.cs`) that takes a factory function (`Func<TViewModel>`) to create the target ViewModel instance and sets it on the `NavigationStore`.
    *   `NavigationServiceFactory`: A helper factory registered with DI (`App.xaml.cs`) that simplifies creating specific `INavigationService<TViewModel>` instances on demand.
    *   `MainViewModel`: The `DataContext` for `MainWindow`. It subscribes to the `NavigationStore` and exposes the `CurrentViewModel` property.
    *   `MainWindow.xaml`: Contains a `ContentControl` bound to `MainViewModel.CurrentViewModel`. `DataTemplate`s are defined in `MainWindow.Resources` (or `App.xaml Resources`) to map ViewModel types to their corresponding View types.
*   **Usage:** To navigate, a ViewModel injects the `NavigationServiceFactory`, creates the required navigation service (e.g., `_navigationServiceFactory.Create<CarsViewModel>()`), and calls its `Navigate()` method.

    ```csharp
    // Inside a ViewModel (e.g., DashboardViewModel)
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly App.NavigationServiceFactory _navFactory;
        public DashboardViewModel(App.NavigationServiceFactory navFactory) { _navFactory = navFactory; }

        [RelayCommand]
        private void GoToCars()
        {
            _navFactory.Create<CarsViewModel>().Navigate();
        }
    }
    ```

## 5. Services

*   Services encapsulate specific application functionalities, often cross-cutting concerns or interactions with external systems (like databases). They reside in the `Services` folder.
*   **Interfaces:** Define contracts for services (e.g., `IAuthenticationService`, `IDataService<T>`, `ICarService`).
*   **Implementations:** Provide the concrete logic (e.g., `MySqlAuthenticationService`, `MySqlDataService<T>`, `MySqlCarService`).
*   **Usage:** Services are registered with the DI container in `App.xaml.cs` and injected into ViewModels or other services that need them. This promotes testability and separation of concerns. Data services handle all database interactions (CRUD).

## 6. Stores

*   Stores manage shared application state that needs to be accessed or modified by multiple ViewModels or services. They reside in the `Stores` folder.
*   They are typically registered as **Singletons** in the DI container.
*   They should implement `INotifyPropertyChanged` (usually by inheriting from `ObservableObject`) if ViewModels need to react to state changes within the store.
*   *Example:* `NavigationStore` (holds the current view), `UserStore` (could hold the currently logged-in user's details).

## 7. Commands

*   User interactions in the View (like button clicks) are handled by Commands exposed by the ViewModel. Views bind UI elements (e.g., `Button.Command`) to these Command properties.
*   We primarily use `RelayCommand` and `AsyncRelayCommand` from the `CommunityToolkit.Mvvm` library. These simplify `ICommand` implementation.
*   Commands encapsulate the action to be performed and logic to determine if the action can be performed (`CanExecute`).
    *   *Example:* `LoginViewModel` has a `LoginCommand` (an `AsyncRelayCommand`) bound to the Login button. Its `CanExecute` logic checks if username/password are provided and if login isn't already in progress.

## 8. Data Access (MySQL)

*   The application connects to a MySQL database.
*   The `MySql.Data` (or `MySqlConnector`) NuGet package is used for connectivity.
*   **Data Services:** All database interaction logic (CRUD - Create, Read, Update, Delete) is encapsulated within specific data service classes in the `Services` folder (e.g., `MySqlCarService`, `MySqlCustomerService`). These services use `MySqlConnection`, `MySqlCommand`, and `MySqlDataReader`.
*   **Connection String:** Managed via `appsettings.json` and accessed using `IConfiguration` injected into data services (best practice) or hardcoded (simpler for demos, avoid in production).
*   **Security:**
    *   Use parameterized queries (`@parameterName`) to prevent SQL injection vulnerabilities.
    *   Store **hashed** passwords (e.g., using BCrypt.Net), never plain text.

## 9. UI Feedback

*   Provide feedback to the user for ongoing operations or errors.
*   **Loading Indicators:** Use properties in the ViewModel (e.g., `IsLoading`, `IsSaving`) bound to the `Visibility` or `IsEnabled` state of UI elements (like progress rings or disabling buttons).
*   **Error Messages:** Use properties in the ViewModel (e.g., `ErrorMessage`) bound to `TextBlock` elements in the View. Clear these messages when a new operation starts.
*   **Consider:** For more complex feedback (dialogs, toasts), a dedicated `IUserInteractionService` or similar service could be implemented and injected into ViewModels to decouple them from specific UI frameworks (like `MessageBox`).

## 10. Publishing

Options for distributing the application:

1.  **Framework-Dependent:** Requires the target machine to have the correct .NET Runtime installed. Produces smaller deployment packages.
    *   Command: `dotnet publish -c Release -r win-x64 --no-self-contained` (replace `win-x64` if needed)

2.  **Self-Contained:** Includes the .NET Runtime with the application. Larger package size, but doesn't require pre-installed runtime.
    *   Command: `dotnet publish -c Release -r win-x64 --self-contained true`

3.  **Single File Executable:** Packages the application and dependencies (optionally including the runtime) into a single `.exe` file. Simplifies distribution. Can be combined with Framework-Dependent or Self-Contained.
    *   Add `/p:PublishSingleFile=true` to the publish command.
    *   Example (Self-Contained Single File): `dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true`

4.  **Installers:** For a more professional installation experience (shortcuts, registry entries, prerequisites):
    *   **MSIX:** Modern Windows packaging format. Good integration with Windows Store and enterprise deployment. Use the "Windows Application Packaging Project" in Visual Studio.
    *   **ClickOnce:** Older Microsoft technology for web-based deployment and updates.
    *   **WiX Toolset:** Powerful open-source toolset for creating traditional MSI installers. Steeper learning curve.

Choose the publishing method based on deployment needs and target audience. For internal use, framework-dependent or self-contained single files are often sufficient. For wider distribution, MSIX or WiX might be better.