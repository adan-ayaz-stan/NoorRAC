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

namespace NoorRAC.Views
{
    /// <summary>
    /// Interaction logic for Customers.xaml
    /// </summary>
    /// 
    public class CustomerRecord
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string CNIC { get; set; }
        public string ContactInfo { get; set; }
        public int TotalRentals { get; set; }
        public decimal OutstandingPayments { get; set; }
        public string DateJoined { get; set; }
    }

    public partial class Customers : UserControl
    {

        public List<CustomerRecord> CustomerRecords { get; set; }

        public Customers()
        {
            InitializeComponent();

            CustomerRecords = new List<CustomerRecord>
            {
               new CustomerRecord { ID = 1, Name = "Ali Khan", CNIC = "12345-6789012-3", ContactInfo = "0300-1234567", TotalRentals = 5, OutstandingPayments = 1500.00m, DateJoined = "2022-01-15" },
               new CustomerRecord { ID = 2, Name = "Sara Ahmed", CNIC = "23456-7890123-4", ContactInfo = "0321-7654321", TotalRentals = 3, OutstandingPayments = 0.00m, DateJoined = "2022-06-10" },
               new CustomerRecord { ID = 3, Name = "Usman Tariq", CNIC = "34567-8901234-5", ContactInfo = "0312-8765432", TotalRentals = 8, OutstandingPayments = 2300.50m, DateJoined = "2023-03-22" },
               new CustomerRecord { ID = 4, Name = "Zainab Rauf", CNIC = "45678-9012345-6", ContactInfo = "0333-5678910", TotalRentals = 1, OutstandingPayments = 500.00m, DateJoined = "2023-09-05" },
               new CustomerRecord { ID = 5, Name = "Ahmed Ali", CNIC = "56789-0123456-7", ContactInfo = "0345-6789012", TotalRentals = 2, OutstandingPayments = 750.00m, DateJoined = "2021-11-20" },
               new CustomerRecord { ID = 6, Name = "Fatima Noor", CNIC = "67890-1234567-8", ContactInfo = "0301-2345678", TotalRentals = 4, OutstandingPayments = 1200.00m, DateJoined = "2022-08-15" },
               new CustomerRecord { ID = 7, Name = "Hassan Raza", CNIC = "78901-2345678-9", ContactInfo = "0322-3456789", TotalRentals = 6, OutstandingPayments = 1800.00m, DateJoined = "2023-01-10" },
               new CustomerRecord { ID = 8, Name = "Ayesha Khan", CNIC = "89012-3456789-0", ContactInfo = "0313-4567890", TotalRentals = 7, OutstandingPayments = 2100.00m, DateJoined = "2023-05-25" },
               new CustomerRecord { ID = 9, Name = "Bilal Ahmed", CNIC = "90123-4567890-1", ContactInfo = "0334-5678901", TotalRentals = 3, OutstandingPayments = 0.00m, DateJoined = "2022-12-05" },
               new CustomerRecord { ID = 10, Name = "Nida Tariq", CNIC = "01234-5678901-2", ContactInfo = "0346-6789012", TotalRentals = 9, OutstandingPayments = 2750.00m, DateJoined = "2023-07-18" },
               new CustomerRecord { ID = 11, Name = "Omar Farooq", CNIC = "12345-6789012-3", ContactInfo = "0302-7890123", TotalRentals = 2, OutstandingPayments = 500.00m, DateJoined = "2021-09-12" },
               new CustomerRecord { ID = 12, Name = "Sana Malik", CNIC = "23456-7890123-4", ContactInfo = "0323-8901234", TotalRentals = 5, OutstandingPayments = 1500.00m, DateJoined = "2022-03-30" },
               new CustomerRecord { ID = 13, Name = "Zahid Iqbal", CNIC = "34567-8901234-5", ContactInfo = "0314-9012345", TotalRentals = 1, OutstandingPayments = 250.00m, DateJoined = "2023-02-14" },
               new CustomerRecord { ID = 14, Name = "Rabia Aslam", CNIC = "45678-9012345-6", ContactInfo = "0335-0123456", TotalRentals = 8, OutstandingPayments = 2000.00m, DateJoined = "2023-06-22" },
               new CustomerRecord { ID = 15, Name = "Fahad Khan", CNIC = "56789-0123456-7", ContactInfo = "0347-1234567", TotalRentals = 4, OutstandingPayments = 1000.00m, DateJoined = "2022-10-01" },
               new CustomerRecord { ID = 16, Name = "Hina Riaz", CNIC = "67890-1234567-8", ContactInfo = "0303-2345678", TotalRentals = 6, OutstandingPayments = 1750.00m, DateJoined = "2023-04-12" },
               new CustomerRecord { ID = 17, Name = "Kamran Shah", CNIC = "78901-2345678-9", ContactInfo = "0324-3456789", TotalRentals = 3, OutstandingPayments = 0.00m, DateJoined = "2022-07-19" },
               new CustomerRecord { ID = 18, Name = "Mariam Ali", CNIC = "89012-3456789-0", ContactInfo = "0315-4567890", TotalRentals = 7, OutstandingPayments = 2200.00m, DateJoined = "2023-08-08" },
               new CustomerRecord { ID = 19, Name = "Noman Qureshi", CNIC = "90123-4567890-1", ContactInfo = "0336-5678901", TotalRentals = 5, OutstandingPayments = 1500.00m, DateJoined = "2022-05-25" },
               new CustomerRecord { ID = 20, Name = "Sadia Javed", CNIC = "01234-5678901-2", ContactInfo = "0348-6789012", TotalRentals = 10, OutstandingPayments = 3000.00m, DateJoined = "2023-09-30" }
            };

            DataContext = this;
        }

    }
}
