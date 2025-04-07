using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

public class Car : INotifyPropertyChanged
{
    private string _status = "Available for rent";
    private string _statusBackground = "#141F15"; // Default background for status
    private string _statusForeground = "#7ED4AD"; // Default foreground for status
    private string _licensePlate = "AJK-838";
    private string _modelInfo = "Toyota Corolla 2011";
    private string _rentPerDay = "Rs. 3500";
    private string _totalRentals = "258";
    private string _avgRentalIncome = "Rs. 30,000";

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string StatusBackground // Bind Border Background to this
    {
        get => _statusBackground;
        set => SetProperty(ref _statusBackground, value);
    }
    public string StatusForeground // Bind TextBlock Foreground to this
    {
        get => _statusForeground;
        set => SetProperty(ref _statusForeground, value);
    }

    public string LicensePlate
    {
        get => _licensePlate;
        set => SetProperty(ref _licensePlate, value);
    }

    public string ModelInfo
    {
        get => _modelInfo;
        set => SetProperty(ref _modelInfo, value);
    }

    public string RentPerDay
    {
        get => _rentPerDay;
        set => SetProperty(ref _rentPerDay, value);
    }

    public string TotalRentals
    {
        get => _totalRentals;
        set => SetProperty(ref _totalRentals, value);
    }

    public string AvgRentalIncome
    {
        get => _avgRentalIncome;
        set => SetProperty(ref _avgRentalIncome, value);
    }

    // --- INotifyPropertyChanged Implementation ---
    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value)) return false;
        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    // --- End INotifyPropertyChanged ---

    // You would also add Commands here if using MVVM for the buttons
    // public ICommand EditCommand { get; }
    // public ICommand SeeMoreCommand { get; }
}

namespace NoorRAC.Views
{
    /// <summary>
    /// Interaction logic for Cars.xaml
    /// </summary>
    public partial class Cars : UserControl
    {
        public ObservableCollection<Car> CarList { get; set; } = new ObservableCollection<Car>();

        public Cars()
        {
            InitializeComponent();
            LoadSampleCars(5); // Load 5 cars for the example

            // Set the DataContext if not already set (crucial for Binding)
            // If using code-behind:
            this.DataContext = this;
        }

        private void LoadSampleCars(int count)
        {
            for (int i = 1; i <= count; i++)
            {
                // Create slightly different data for each car
                CarList.Add(new Car
                {
                    Status = (i % 2 == 0) ? "Rented" : "Available for rent",
                    StatusBackground = (i % 2 == 0) ? "#F6DC43" : "#141F15",
                    StatusForeground = (i % 2 == 0) ? "Black" : "#7ED4AD",
                    LicensePlate = $"XYZ-{100 + i}",
                    ModelInfo = $"Car Model {2010 + i}",
                    RentPerDay = $"Rs. {3000 + (i * 100)}",
                    TotalRentals = $"{100 + (i * 20)}",
                    AvgRentalIncome = $"Rs. {25000 + (i * 1000)}"
                });
            }
        }
    }
}
