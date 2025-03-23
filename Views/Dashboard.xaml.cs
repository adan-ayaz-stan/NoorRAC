using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

public class RentalRecord
{
    public int ID { get; set; }
    public string ClientName { get; set; }
    public string CarType { get; set; }
    public string CarNumber { get; set; }
    public string Status { get; set; }
}

namespace NoorRAC.Views
{
    /// <summary>
    /// Interaction logic for Dashboard.xaml
    /// </summary>
    public partial class Dashboard : UserControl
    {
        public ObservableCollection<RentalRecord> RentalRecords { get; set; }

        public Dashboard()
        {
            InitializeComponent();
            RentalRecords = new ObservableCollection<RentalRecord> {
                new RentalRecord { ID = 1, ClientName = "Adan Ayaz", CarType = "Sedan", CarNumber = "AJK-838", Status = "Active" },
            new RentalRecord { ID = 2, ClientName = "Spitfire Kasnoviz", CarType = "SUV", CarNumber = "CHC-984", Status = "Active" },
            new RentalRecord { ID = 3, ClientName = "Alpha Omega", CarType = "Sedan", CarNumber = "ACV-345", Status = "Active" },
            new RentalRecord { ID = 4, ClientName = "Schnapps", CarType = "SUV", CarNumber = "VAC-496", Status = "Active" },
            new RentalRecord { ID = 5, ClientName = "Scooby Doo", CarType = "Sedan", CarNumber = "DCJ-123", Status = "Active" },
            };

            DataContext = this;
        }



    }
}
