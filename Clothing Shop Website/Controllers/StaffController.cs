using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Clothing_Shop_Website.Data;
using Clothing_Shop_Website.Enums;
using Clothing_Shop_Website.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clothing_Shop_Website.Controllers
{
    public class StaffController : Controller
    {
        private readonly AppDbContext _db;
        private readonly ICubeMdxAnalyticsService _cube;
        private readonly IWebHostEnvironment _env;

        public StaffController(AppDbContext db, ICubeMdxAnalyticsService cube, IWebHostEnvironment env)
        {
            _db = db;
            _cube = cube;
            _env = env;
        }

        private bool IsStaff() => HttpContext.Session.GetInt32("Role") == (int)UserRole.Staff;

        public IActionResult Index() => RedirectToAction("Inventory");

        // KHO HÀNG
        public async Task<IActionResult> Inventory(string? search, int? categoryId)
        {
            if (!IsStaff()) return RedirectToAction("Login", "Account");

            // Sử dụng AsNoTracking
            var querry = _db.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                .AsQueryable();

            // Bộ lọc
            if (!string.IsNullOrEmpty(search))
                querry = querry.Where(p => p.ProductName.Contains(search.Trim()));

            if (!categoryId.HasValue)
                querry = querry.Where(p => p.CategoryID == categoryId);

            // Gửi danh sách sang View
            ViewBag.Categories = await _db.Categories.AsNoTracking().OrderBy(c => c.CategoryName).ToListAsync();

            // Giữ trạng thái lọc cũ trên giao diện
            ViewBag.Search = search;
            ViewBag.CagoryId = categoryId;

            return View(await querry.OrderByDescending(p => p.ProductID).ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(Dictionary<int, int> duLieuTonKho)
        {
            if (!IsStaff())
                return RedirectToAction("Login", "Account");

            if(duLieuTonKho == null || !duLieuTonKho.Any())
            {
                TempData["Error"] = "Khong co du lieu ton kho nao duoc gui len";
                return RedirectToAction("Inventory");
            }

            int updated = 0;
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                foreach(var item in duLieuTonKho)
                {
                    int maKichCo = item.Key;
                    int soLuongMoi = item.Value;

                    if (soLuongMoi <= 0)
                        throw new Exception("Ton kho khong duoc nho hon 0");

                    var size = await _db.ProductSizes.FindAsync(maKichCo);
                    if(size != null)
                    {
                        size.StockQuantity += soLuongMoi;
                        updated++;
                    }
                }
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                if (updated > 0)
                    TempData["Success"] = $"Da cap nhat thanh cong {updated} dong ton kho";
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["Error"] = "Lỗi cập nhật tồn kho: " + ex.Message;
            }
            return RedirectToAction("Inventory");
        }

        // ═══════════════════════════════
        //   KHÁCH HÀNG
        // ═══════════════════════════════
        public async Task<IActionResult> Customers(string? search)
        {
            if (!IsStaff()) return RedirectToAction("Login", "Account");

            var query = _db.Users
                .Include(u => u.CustomerDetail)
                .Include(u => u.Orders)
                .Where(u => u.Role == 2)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(u => u.FullName.Contains(search) || u.Phone.Contains(search));

            ViewBag.Search = search;
            return View(await query.OrderByDescending(u => u.UserID).ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLockUser(int userId)
        {
            if (!IsStaff()) return RedirectToAction("Login", "Account");

            var user = await _db.Users.FindAsync(userId);
            if (user != null)
            {
                user.Status = user.Status == 1 ? 0 : 1;
                await _db.SaveChangesAsync();
                TempData["Success"] = user.Status == 0 ? "Đã khóa tài khoản!" : "Đã mở khóa tài khoản!";
            }
            return RedirectToAction("Customers");
        }

        // ═══════════════════════════════
        //   ĐƠN HÀNG
        // ═══════════════════════════════
        

        // ═══════════════════════════════
        //   THỐNG KÊ
        // ═══════════════════════════════
        public async Task<IActionResult> Statistics()
        {
            if (!IsStaff()) return RedirectToAction("Login", "Account");

            try
            {
                var vm = await _cube.BuildDashboardAsync(null, null);
                return View(vm);
            }
            catch
            {
                return View(new Clothing_Shop_Website.ViewModels.AdminDashboardViewModel
                {
                    CubeError = "Không kết nối được Cube. Hiển thị dữ liệu trực tiếp."
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportReport()
        {
            if (!IsStaff()) return RedirectToAction("Login", "Account");

            // Lấy dữ liệu đơn hàng trong 30 ngày gần nhất
            var since = DateTime.Now.AddDays(-30);
            var orders = await _db.Orders
                .Include(o => o.OrderDetails)
                .Where(o => o.OrderDate >= since)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("BÁO CÁO DOANH THU - NEVA CLOTHING");
            sb.AppendLine($"Từ ngày,{since:dd/MM/yyyy}");
            sb.AppendLine($"Đến ngày,{DateTime.Now:dd/MM/yyyy}");
            sb.AppendLine($"Xuất lúc,{DateTime.Now:dd/MM/yyyy HH:mm}");
            sb.AppendLine();
            sb.AppendLine("Mã đơn,Ngày đặt,Khách hàng,SĐT,Địa chỉ,Tổng tiền,Trạng thái");

            string[] statusLabels = { "Chờ duyệt", "Đang giao", "Đã giao", "Đã hủy", "Đang hoàn chuyển" };
            decimal grandTotal = 0;
            foreach (var o in orders)
            {
                grandTotal += o.TotalAmount;
                var statusLabel = o.Status >= 0 && o.Status < statusLabels.Length ? statusLabels[o.Status] : "Không rõ";
                sb.AppendLine($"NV-{o.OrderID:D8},{o.OrderDate:dd/MM/yyyy HH:mm},\"{o.ReceiverName}\",{o.ReceiverPhone},\"{o.ShippingAddress} - {o.ShippingProvince}\",{o.TotalAmount:N0},{statusLabel}");
            }
            sb.AppendLine();
            sb.AppendLine($"Tổng doanh thu (bao gồm mọi TT):,{grandTotal:N0}");
            sb.AppendLine($"Tổng đơn hàng:,{orders.Count}");

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv; charset=utf-8",
                $"BaoCaoDoanhThu_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }
    }

    // ViewModel for bulk stock update
    public class StockUpdateRow
    {
        public int SizeId { get; set; }
        public int NewQty { get; set; }
    }
}
