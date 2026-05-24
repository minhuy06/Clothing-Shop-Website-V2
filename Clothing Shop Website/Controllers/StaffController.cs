using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Clothing_Shop_Website.Data;
using Clothing_Shop_Website.Models;
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

        // Role 0 = Admin, Role 1 = Staff — both can access staff pages
        private bool IsStaff()
        {
            var role = HttpContext.Session.GetInt32("Role");
            return role == 0 || role == 1;
        }

        public IActionResult Index() => RedirectToAction("Inventory");

        // ═══════════════════════════════
        //   KHO HÀNG (INVENTORY)
        // ═══════════════════════════════
        public async Task<IActionResult> Inventory(string? search, int? categoryId)
        {
            if (!IsStaff()) return RedirectToAction("Login", "Account");

            var query = _db.Products
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.ProductName.Contains(search));
            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryID == categoryId);

            ViewBag.Categories = await _db.Categories.OrderBy(c => c.CategoryName).ToListAsync();
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;

            return View(await query.OrderByDescending(p => p.ProductID).ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(List<StockUpdateRow> rows)
        {
            if (!IsStaff()) return RedirectToAction("Login", "Account");

            int updated = 0;
            foreach (var row in rows ?? new List<StockUpdateRow>())
            {
                var size = await _db.ProductSizes.FindAsync(row.SizeId);
                if (size == null) continue;
                size.StockQuantity = Math.Max(0, row.NewQty);
                updated++;
            }
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã cập nhật {updated} dòng tồn kho.";
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
        public async Task<IActionResult> Orders(string? status, string? search)
        {
            if (!IsStaff()) return RedirectToAction("Login", "Account");

            var query = _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && int.TryParse(status, out int s))
                query = query.Where(o => o.Status == s);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(o =>
                    o.ReceiverName.Contains(search) ||
                    o.ReceiverPhone.Contains(search) ||
                    o.OrderID.ToString().Contains(search));

            ViewBag.Status = status;
            ViewBag.Search = search;

            return View(await query.OrderByDescending(o => o.OrderDate).ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, int status)
        {
            if (!IsStaff()) return RedirectToAction("Login", "Account");

            var order = await _db.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = status;
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã cập nhật trạng thái!";
            }
            return RedirectToAction("Orders");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOrder(int orderId)
        {
            if (!IsStaff()) return RedirectToAction("Login", "Account");

            var order = await _db.Orders.FindAsync(orderId);
            if (order != null)
            {
                _db.Orders.Remove(order);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã xóa đơn hàng!";
            }
            return RedirectToAction("Orders");
        }

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
                return View(new Clothing_Shop_Website.Models.ViewModels.AdminDashboardViewModel
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
