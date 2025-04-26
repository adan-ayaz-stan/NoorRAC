using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Collections.ObjectModel;

namespace NoorRAC.Views
{
    public partial class Expenses : UserControl
    {
        public ObservableCollection<ExpensesRecord> ExpensesRecords { get; set; }

        public Expenses()
        {
            InitializeComponent();
            LoadDummyData();
            DataContext = this;
        }

        private void LoadDummyData()
        {
            ExpensesRecords = new ObservableCollection<ExpensesRecord>
            {
                new ExpensesRecord { ID = 1, Date = DateTime.Now.AddDays(-1), Amount = 100.50, Category = "Travel", Description = "Taxi fare", PaymentStatus = "Paid", PaymentMethod = "Credit Card", Rental = "Car A" },
                new ExpensesRecord { ID = 2, Date = DateTime.Now.AddDays(-2), Amount = 200.00, Category = "Food", Description = "Lunch with client", PaymentStatus = "Pending", PaymentMethod = "Cash", Rental = "Car B" },
                new ExpensesRecord { ID = 3, Date = DateTime.Now.AddDays(-3), Amount = 50.75, Category = "Office Supplies", Description = "Stationery", PaymentStatus = "Paid", PaymentMethod = "Debit Card", Rental = "Car C" },
                new ExpensesRecord { ID = 4, Date = DateTime.Now.AddDays(-4), Amount = 300.00, Category = "Maintenance", Description = "Car repair", PaymentStatus = "Pending", PaymentMethod = "Bank Transfer", Rental = "Car D" }
            };
        }
    }

    public class ExpensesRecord
    {
        public int ID { get; set; }
        public DateTime Date { get; set; }
        public double Amount { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string PaymentStatus { get; set; }
        public string PaymentMethod { get; set; }
        public string Rental { get; set; }
    }
}
