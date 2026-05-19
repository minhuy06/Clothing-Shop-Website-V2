using System;
using System.Collections.Generic;

namespace Clothing_Shop_Website.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalSalesLines { get; set; }
        public int DistinctCustomers { get; set; }
        public int DistinctProductsSold { get; set; }

        public List<MonthRevenuePoint> RevenueLastMonths { get; set; } = new List<MonthRevenuePoint>();
        public List<TopSellerCubeRow> TopSellers { get; set; } = new List<TopSellerCubeRow>();
        public List<LabelAmountRow> RevenueByCategory { get; set; } = new List<LabelAmountRow>();
        public List<LabelAmountRow> RevenueByAgeGroup { get; set; } = new List<LabelAmountRow>();

        /// <summary>Ngày có bán gần nhất (MDX / Fact_Sales theo Dim_Time).</summary>
        public List<RecentCubeDayRow> RecentCubeDays { get; set; } = new List<RecentCubeDayRow>();

        public string? CubeError { get; set; }
    }

    public class MonthRevenuePoint
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string Label { get; set; } = "";
        public decimal Revenue { get; set; }
    }

    public class TopSellerCubeRow
    {
        public int SourceProductId { get; set; }
        public int QuantitySold { get; set; }
        public string? ProductName { get; set; }
        public string? CategoryName { get; set; }
        public int CategoryId { get; set; }
        public decimal Price { get; set; }
        public string? SeasonLabel { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class LabelAmountRow
    {
        public string Label { get; set; } = "";
        public decimal Amount { get; set; }
    }

    public class RecentCubeDayRow
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int SalesLines { get; set; }
    }
}
