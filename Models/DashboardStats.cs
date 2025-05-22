// NoorRAC/Models/DashboardStats.cs
namespace NoorRAC.Models
{
    public class FinancialMetric
    {
        public string CurrentPeriodAmount { get; set; } = "Rs. 0";
        public string PercentageChange { get; set; } = "0%"; // e.g., "+15.5%" or "-7.2%"
        public string ChangeDescription { get; set; } = "vs last month"; // Or "vs previous period"
        public bool IsIncrease { get; set; } // For potential color coding (green/red)
    }

    public class DashboardStats
    {
        public FinancialMetric Turnover { get; set; } = new FinancialMetric();
        public FinancialMetric Income { get; set; } = new FinancialMetric();
        public FinancialMetric Outflow { get; set; } = new FinancialMetric();
    }
}