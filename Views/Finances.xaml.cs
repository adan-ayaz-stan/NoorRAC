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
using SkiaSharp;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using System.Collections.ObjectModel;

namespace NoorRAC.Views
{
    public class FinanceRecord
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public double Amount { get; set; }
        public string Type { get; set; }
        public string Date { get; set; }
        public string Action { get; set; }
    }

    /// <summary>
    /// Interaction logic for Finances.xaml
    /// </summary>
    public partial class Finances : UserControl
    {

        private ObservableCollection<FinanceRecord> _financeRecords;
        public ObservableCollection<FinanceRecord> FinanceRecords
        {
            get { return _financeRecords; }
            set { _financeRecords = value; }
        }

        public DateTime ToDate { get; set; }

        public ISeries[] Series { get; set; }
        public Axis[] XAxes { get; set; }
        public Axis[] YAxes { get; set; }

        public Finances()
        {
            InitializeComponent();

            FinanceRecords = new ObservableCollection<FinanceRecord>
            {
                new FinanceRecord { ID = 1, Name = "John Doe", Amount = 100.50, Type = "Income", Date = DateTime.Now.ToShortDateString(), Action = "Approved" },
                new FinanceRecord { ID = 2, Name = "Jane Smith", Amount = 250.00, Type = "Expense", Date = DateTime.Now.ToShortDateString(), Action = "Pending" },
                new FinanceRecord { ID = 3, Name = "Michael Brown", Amount = 75.80, Type = "Advance", Date = DateTime.Now.ToShortDateString(), Action = "Approved" },
                new FinanceRecord { ID = 4, Name = "Emily Johnson", Amount = 300.20, Type = "Expense", Date = DateTime.Now.ToShortDateString(), Action = "Rejected" },
                new FinanceRecord { ID = 5, Name = "David Wilson", Amount = 120.00, Type = "Income", Date = DateTime.Now.ToShortDateString(), Action = "Pending" }
            };

            ToDate = DateTime.Now;

            Series =
            [
                new ColumnSeries<double>
                {
                    Name = "Income",
                    Values = [500, 600, 750, 900, 1100, 1300, 1400, 1350, 1200, 1100, 950, 700], // Monthly income in thousands
                    Fill = new SolidColorPaint(new SKColor(119, 112, 255)),
                    Rx = 4,
                    Ry = 4,
                    MaxBarWidth = 30
                },
                new ColumnSeries<double>
                {
                    Name = "Expenses",
                    Values = [300, 350, 400, 450, 600, 750, 800, 780, 700, 650, 600, 500], // Monthly expenses in thousands
                    Fill = new SolidColorPaint(new SKColor(20, 20, 20)),
                    Rx = 4,
                    Ry = 4,
                    MaxBarWidth = 30
                }
            ];

            XAxes =
            [
                new Axis
                {
                    Labels = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"],
                    LabelsRotation = 0,
                    SeparatorsPaint = new SolidColorPaint(new SKColor(200, 200, 200)),
                    SeparatorsAtCenter = false,
                    TicksPaint = new SolidColorPaint(new SKColor(35, 35, 35)),
                    TicksAtCenter = true,
                    ForceStepToMin = true,
                    MinStep = 1,
                    TextSize = 14
                }
            ];

            YAxes =
            [
                new Axis
                {
                    Labeler = value => $"Rs {value * 1000:N0}",
                    SeparatorsPaint = new SolidColorPaint(new SKColor(200, 200, 200))
                }
            ];

            overviewChart.Series = Series;
            overviewChart.XAxes = XAxes;
            overviewChart.YAxes = YAxes;

            financesTable.ItemsSource = FinanceRecords;
        }
    }
}
