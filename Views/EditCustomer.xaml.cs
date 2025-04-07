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

namespace NoorRAC.Views
{
    /// <summary>
    /// Interaction logic for EditCustomer.xaml
    /// </summary>
    public partial class EditCustomer : UserControl
    {
        public ObservableCollection<RentalRecord> RentalRecords { get; set; }
        public EditCustomer()
        {
            InitializeComponent();
            RentalRecords = new ObservableCollection<RentalRecord>
            {
                new RentalRecord { ID = 1, ClientName = "Toyota Corolla", CarType = "AB123CD", CarNumber = "$50", Status = "Rented", RentalDate = "2025-04-01" },
                new RentalRecord { ID = 2, ClientName = "Honda Civic", CarType = "XY987ZT", CarNumber = "$60", Status = "Available", RentalDate = "2025-03-28" },
                new RentalRecord { ID = 3, ClientName = "Ford Focus", CarType = "LM456OP", CarNumber = "$55", Status = "Rented", RentalDate = "2025-03-25" },
                new RentalRecord { ID = 4, ClientName = "Chevrolet Malibu", CarType = "GH654RT", CarNumber = "$65", Status = "Available", RentalDate = "2025-03-20" },
                new RentalRecord { ID = 5, ClientName = "Nissan Altima", CarType = "UV789WX", CarNumber = "$58", Status = "Rented", RentalDate = "2025-03-15" }

            };
            DataContext = this;
        }
    }
    public class RentalRecord
    {
        public int ID { get; set; }
        public string ClientName { get; set; }
        public string CarType { get; set; }
        public string CarNumber { get; set; }
        public string Status { get; set; }
        public string RentalDate { get; set; }
    }
}
