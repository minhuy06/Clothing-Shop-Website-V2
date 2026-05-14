namespace Clothing_Shop_Website.Options
{
    /// <summary>
    /// Tên measure / dimension hierarchy phải khớp với cube SSDT xuất từ ClothingShop_Cube
    /// (kho dữ liệu DB_ShopQuanAo_DataWarehouse — Fact_Sales, Dim_Product, Dim_Customer, Dim_Time).
    /// </summary>
    public class CubeMdxOptions
    {
        public const string SectionName = "Cube";

        /// <summary>Chuỗi kết nối OLE DB/MSOLAP tới Analysis Services, ví dụ: Provider=MSOLAP;Data Source=localhost;Catalog=ClothingShop_Cube;</summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>Tên cube trong database (mục Cubes trên SSMS).</summary>
        public string CubeName { get; set; } = "ClothingShop_Cube";

        public string MeasureTotalRevenue { get; set; } = "[Measures].[Total Revenue]";
        public string MeasureQuantity { get; set; } = "[Measures].[Quantity]";
        /// <summary>Measure đếm số dòng Fact_Sales do SSAS tạo (tên mặc định thường là Fact Sales Count).</summary>
        public string MeasureFactSalesCount { get; set; } = "[Measures].[Fact Sales Count]";

        public string DimProduct { get; set; } = "[Dim Product]";
        public string DimCustomer { get; set; } = "[Dim Customer]";
        public string DimTime { get; set; } = "[Dim Time]";

        public string HierarchySourceProductId { get; set; } = "[Dim Product].[Source Product ID].[Source Product ID]";
        public string HierarchyCategoryName { get; set; } = "[Dim Product].[Category Name].[Category Name]";
        public string HierarchySeason { get; set; } = "[Dim Product].[Season].[Season]";
        public string HierarchyAgeGroup { get; set; } = "[Dim Customer].[Age Group].[Age Group]";
        public string HierarchyMonth { get; set; } = "[Dim Time].[Month].[Month]";
        public string HierarchyYear { get; set; } = "[Dim Time].[Year].[Year]";
        /// <summary>Ngày đầy đủ trên Dim_Time (dùng gom doanh thu theo tháng).</summary>
        public string HierarchyFullDate { get; set; } = "[Dim Time].[Full Date].[Full Date]";
        public string HierarchyCustomerKey { get; set; } = "[Dim Customer].[Customer Key].[Customer Key]";
    }
}
