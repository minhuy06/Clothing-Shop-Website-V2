using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clothing_Shop_Website.Data;
using Clothing_Shop_Website.Models.ViewModels;
using Clothing_Shop_Website.Options;
using Microsoft.AnalysisServices.AdomdClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Clothing_Shop_Website.Services
{
    public class CubeMdxAnalyticsService : ICubeMdxAnalyticsService
    {
        private readonly CubeMdxOptions _opt;
        private readonly AppDbContext _db;
        private readonly ILogger<CubeMdxAnalyticsService> _log;

        public CubeMdxAnalyticsService(IOptions<CubeMdxOptions> opt, AppDbContext db, ILogger<CubeMdxAnalyticsService> log)
        {
            _opt = opt.Value;
            _db = db;
            _log = log;
        }

        public async Task<AdminDashboardViewModel> BuildDashboardAsync(string? seasonFilter, string? ageGroupFilter, CancellationToken cancellationToken = default)
        {
            var vm = new AdminDashboardViewModel();
            if (string.IsNullOrWhiteSpace(_opt.ConnectionString))
            {
                vm.CubeError = "Chưa cấu hình mục \"Cube:ConnectionString\" trong appsettings (Provider=MSOLAP;Data Source=...;Catalog=ClothingShop_Cube).";
                return vm;
            }

            try
            {
                using (var conn = new AdomdConnection(_opt.ConnectionString))
                {
                    await Task.Run(() => conn.Open(), cancellationToken);

                    await FillKpisAsync(conn, vm, cancellationToken);
                    await FillRevenueByMonthAsync(conn, vm, cancellationToken);
                    await FillRevenueByCategoryAsync(conn, vm, cancellationToken);
                    await FillRevenueByAgeGroupAsync(conn, vm, cancellationToken);
                    await FillTopSellersAsync(conn, vm, seasonFilter, ageGroupFilter, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "MDX / Analysis Services không khả dụng.");
                vm.CubeError = "Không truy vấn được cube (kiểm tra SSAS đã deploy ClothingShop_Cube và chuỗi kết nối). Chi tiết: " + ex.Message;
            }

            return vm;
        }

        private string WhereBestSellerSlice(string? seasonNum, string? ageGroup)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(seasonNum) && int.TryParse(seasonNum, out var sn) && sn is >= 1 and <= 4)
            {
                var vn = sn switch { 1 => "Spring", 2 => "Summner", 3 => "Autumn", 4 => "Winter", _ => "" };
                parts.Add($"{_opt.HierarchySeason}.[{MdxEscapeName(vn)}]");
            }
            if (!string.IsNullOrWhiteSpace(ageGroup))
            {
                parts.Add($"{_opt.HierarchyAgeGroup}.[{MdxEscapeName(ageGroup.Trim())}]");
            }
            return parts.Count == 0 ? "" : " WHERE ( " + string.Join(", ", parts) + " ) ";
        }

        private static string MdxEscapeName(string name) => name.Replace("]", "]]");

        private async Task FillKpisAsync(AdomdConnection conn, AdminDashboardViewModel vm, CancellationToken ct)
        {
            var mdx =
                "WITH MEMBER [Measures].[__DCust] AS DISTINCTCOUNT(" + _opt.HierarchyCustomerKey + ".Members) " +
                "MEMBER [Measures].[__DProd] AS DISTINCTCOUNT(" + _opt.HierarchySourceProductId + ".Members) " +
                "SELECT { " + _opt.MeasureTotalRevenue + ", " + _opt.MeasureFactSalesCount + ", [Measures].[__DCust], [Measures].[__DProd] } ON COLUMNS " +
                "FROM [" + MdxEscapeName(_opt.CubeName) + "]";
            var cs = ExecuteCellSet(conn, mdx);
            if (cs.Axes.Count == 0 || cs.Axes[0].Positions.Count < 4) return;
            vm.TotalRevenue = ToDecimal(Cell(cs, 0, 0));
            vm.TotalSalesLines = ToInt(Cell(cs, 1, 0));
            vm.DistinctCustomers = ToInt(Cell(cs, 2, 0));
            vm.DistinctProductsSold = ToInt(Cell(cs, 3, 0));
            await Task.CompletedTask;
        }

        private async Task FillRevenueByMonthAsync(AdomdConnection conn, AdminDashboardViewModel vm, CancellationToken ct)
        {
            var mdx =
                "SELECT { " + _opt.MeasureTotalRevenue + ", " + _opt.MeasureFactSalesCount + " } ON COLUMNS, " +
                "NON EMPTY { " + _opt.HierarchyFullDate + ".Members } ON ROWS " +
                "FROM [" + MdxEscapeName(_opt.CubeName) + "]";
            var cs = ExecuteCellSet(conn, mdx);
            if (cs.Axes.Count < 2) return;

            var agg = new Dictionary<(int Y, int M), (decimal Rev, int Lines)>();
            var byDay = new List<RecentCubeDayRow>();
            var rowAxis = cs.Axes[1];
            for (var r = 0; r < rowAxis.Positions.Count; r++)
            {
                if (!TryParseDateMember(rowAxis.Positions[r].Members[0], out var dt)) continue;
                var rev = ToDecimal(Cell(cs, 0, r));
                var lines = ToInt(Cell(cs, 1, r));
                var key = (dt.Year, dt.Month);
                agg.TryGetValue(key, out var cur);
                agg[key] = (cur.Rev + rev, cur.Lines + lines);
                byDay.Add(new RecentCubeDayRow { Date = dt.Date, Revenue = rev, SalesLines = lines });
            }

            var last6 = agg
                .OrderBy(x => x.Key.Y).ThenBy(x => x.Key.M)
                .TakeLast(6)
                .ToList();

            foreach (var e in last6)
            {
                vm.RevenueLastMonths.Add(new MonthRevenuePoint
                {
                    Year = e.Key.Y,
                    Month = e.Key.M,
                    Label = "T" + e.Key.M,
                    Revenue = e.Value.Rev
                });
            }

            vm.RecentCubeDays = byDay
                .Where(d => d.Revenue > 0 || d.SalesLines > 0)
                .GroupBy(d => d.Date)
                .Select(g => new RecentCubeDayRow
                {
                    Date = g.Key,
                    Revenue = g.Sum(x => x.Revenue),
                    SalesLines = g.Sum(x => x.SalesLines)
                })
                .OrderByDescending(x => x.Date)
                .Take(5)
                .ToList();

            await Task.CompletedTask;
        }

        private async Task FillRevenueByCategoryAsync(AdomdConnection conn, AdminDashboardViewModel vm, CancellationToken ct)
        {
            var mdx =
                "SELECT { " + _opt.MeasureTotalRevenue + " } ON COLUMNS, " +
                "NON EMPTY { " + _opt.HierarchyCategoryName + ".Members } ON ROWS " +
                "FROM [" + MdxEscapeName(_opt.CubeName) + "]";
            var cs = ExecuteCellSet(conn, mdx);
            if (cs.Axes.Count < 2) return;
            var rows = cs.Axes[1];
            for (var r = 0; r < rows.Positions.Count; r++)
            {
                var cap = rows.Positions[r].Members[0].Caption;
                if (string.IsNullOrWhiteSpace(cap) || cap.Equals("All", StringComparison.OrdinalIgnoreCase)) continue;
                vm.RevenueByCategory.Add(new LabelAmountRow { Label = cap, Amount = ToDecimal(Cell(cs, 0, r)) });
            }
            await Task.CompletedTask;
        }

        private async Task FillRevenueByAgeGroupAsync(AdomdConnection conn, AdminDashboardViewModel vm, CancellationToken ct)
        {
            var mdx =
                "SELECT { " + _opt.MeasureTotalRevenue + " } ON COLUMNS, " +
                "NON EMPTY { " + _opt.HierarchyAgeGroup + ".Members } ON ROWS " +
                "FROM [" + MdxEscapeName(_opt.CubeName) + "]";
            var cs = ExecuteCellSet(conn, mdx);
            if (cs.Axes.Count < 2) return;
            var rows = cs.Axes[1];
            for (var r = 0; r < rows.Positions.Count; r++)
            {
                var cap = rows.Positions[r].Members[0].Caption;
                if (string.IsNullOrWhiteSpace(cap) || cap.Equals("All", StringComparison.OrdinalIgnoreCase)) continue;
                vm.RevenueByAgeGroup.Add(new LabelAmountRow { Label = cap, Amount = ToDecimal(Cell(cs, 0, r)) });
            }
            await Task.CompletedTask;
        }

        private async Task FillTopSellersAsync(AdomdConnection conn, AdminDashboardViewModel vm, string? season, string? ageGroup, CancellationToken ct)
        {
            var where = WhereBestSellerSlice(season, ageGroup);
            var mdx =
                "SELECT { " + _opt.MeasureQuantity + " } ON COLUMNS, " +
                "NON EMPTY TOPCOUNT( { " + _opt.HierarchySourceProductId + ".Members }, 8, " + _opt.MeasureQuantity + " ) ON ROWS " +
                "FROM [" + MdxEscapeName(_opt.CubeName) + "]" + where;
            var cs = ExecuteCellSet(conn, mdx);
            if (cs.Axes.Count < 2) return;

            var rowAxis = cs.Axes[1];
            for (var r = 0; r < rowAxis.Positions.Count; r++)
            {
                var m = rowAxis.Positions[r].Members[0];
                if (!TryParseIntKey(m.Caption, out var pid) && !TryParseIntKey(m.Name, out pid)) continue;
                var qty = ToInt(Cell(cs, 0, r));
                if (qty <= 0) continue;
                vm.TopSellers.Add(new TopSellerCubeRow { SourceProductId = pid, QuantitySold = qty });
            }

            if (vm.TopSellers.Count == 0) return;

            var ids = vm.TopSellers.Select(t => t.SourceProductId).Distinct().ToList();

            var products = await _db.Products.AsNoTracking()
                .Include(p => p.Category)
                .Where(p => ids.Contains(p.ProductID))
                .ToListAsync(ct);

            string SeasonLabel(int s) => s switch { 1 => "Xuân", 2 => "Hạ", 3 => "Thu", 4 => "Đông", _ => "" };

            foreach (var row in vm.TopSellers)
            {
                var p = products.FirstOrDefault(x => x.ProductID == row.SourceProductId);
                if (p == null) continue;
                row.ProductName = p.ProductName;
                row.CategoryName = p.Category?.CategoryName;
                row.CategoryId = p.CategoryID;
                row.Price = p.Price;
                row.OriginalPrice = p.OriginalPrice;
                row.SeasonLabel = SeasonLabel(p.Session);
                row.ImageUrl = p.ImageUrl;
            }

            vm.TopSellers = vm.TopSellers.Where(x => x.ProductName != null).ToList();
        }

        private static CellSet ExecuteCellSet(AdomdConnection conn, string mdx)
        {
            using (var cmd = new AdomdCommand(mdx, conn))
            {
                cmd.CommandTimeout = 120;
                return cmd.ExecuteCellSet();
            }
        }

        private static Cell Cell(CellSet cs, int columnOrdinal, int rowOrdinal) => cs[columnOrdinal, rowOrdinal];

        private static decimal ToDecimal(object? v)
        {
            if (v == null || v is DBNull) return 0m;
            return Convert.ToDecimal(v, CultureInfo.InvariantCulture);
        }

        private static int ToInt(object? v)
        {
            if (v == null || v is DBNull) return 0;
            return Convert.ToInt32(Math.Round(Convert.ToDouble(v, CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture);
        }

        private static bool TryParseDateMember(Member member, out DateTime dt)
        {
            dt = default;
            var name = member.Name;
            var idx = name.IndexOf("&[", StringComparison.Ordinal);
            if (idx >= 0)
            {
                var end = name.IndexOf(']', idx + 2);
                if (end > idx && int.TryParse(name.AsSpan(idx + 2, end - idx - 2), out var timeKey) && timeKey > 19000101 && timeKey < 30000101)
                {
                    var y = timeKey / 10000;
                    var mo = timeKey / 100 % 100;
                    var d = timeKey % 100;
                    try { dt = new DateTime(y, mo, d); return true; } catch { return false; }
                }
            }
            return DateTime.TryParse(member.Caption, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
        }

        private static bool TryParseIntKey(string? text, out int id)
        {
            id = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out id)) return true;
            var idx = text.IndexOf("&[", StringComparison.Ordinal);
            if (idx >= 0)
            {
                var end = text.IndexOf(']', idx + 2);
                if (end > idx && int.TryParse(text.AsSpan(idx + 2, end - idx - 2), out id)) return true;
            }
            return false;
        }
    }
}
