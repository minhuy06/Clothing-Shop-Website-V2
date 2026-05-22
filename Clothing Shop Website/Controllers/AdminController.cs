using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Clothing_Shop_Website.Data;
using Clothing_Shop_Website.Models;
using Clothing_Shop_Website.ViewModels;
using Clothing_Shop_Website.Services;

namespace Clothing_Shop_Website.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;
        private readonly ICubeMdxAnalyticsService _cube;
        private readonly IWebHostEnvironment _env;

        public AdminController(AppDbContext db, ICubeMdxAnalyticsService cube, IWebHostEnvironment env)
        {
            _db = db;
            _cube = cube;
            _env = env;
        }

        // Bảo mật dùng chung cho các hàm dưới
        private bool IsAdmin() => HttpContext.Session.GetInt32("Role") == 0;

        // Phân tích dữ liệu
        public async Task<IActionResult> Dashboard(string? season, string? ageGroup)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var vm = await _cube.BuildDashboardAsync(season, ageGroup);
            ViewBag.SeasonFilter = season;
            ViewBag.AgeGroupFilter = ageGroup;
            ViewBag.Categories = await _db.Categories.AsNoTracking().OrderBy(c => c.CategoryName).ToListAsync();

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> DashboardData(string? season, string? ageGroup)
        {
            if (!IsAdmin()) return Unauthorized();

            var vm = await _cube.BuildDashboardAsync(season, ageGroup);

            // Trả dữ liệu thô dạng JSON
            return Json(new
            {
                vm.CubeError, // Thông báo lỗi Cube nếu có
                kpi = new { vm.TotalRevenue, vm.TotalSalesLines, vm.DistinctCustomers, vm.DistinctProductsSold }, // Cụm 4 chỉ số KPI tổng quan
                revenue6m = vm.RevenueLastMonths,
                top = vm.TopSellers,
                catPie = vm.RevenueByCategory,
                ageRev = vm.RevenueByAgeGroup
            });
        }

        // Lấy danh mục có doanh thu cao nhất
        [HttpGet]
        public async Task<IActionResult> GetAIPrediction()
        {
            try
            {
                // Gọi mô hình dự báo DMX từ tầng Service
                var predictions = await _cube.GetForecastNext3MonthsAsync();

                if (predictions == null || predictions.Count == 0)
                    return Json(new { success = false, message = "Chưa đủ dữ liệu chuỗi thời gian để dự báo" });

                // Sắp xếp giảm dần theo số lượng dự báo bán ra để tìm ra danh mục có xu hướng tăng trưởng mạnh nhất
                var topCategory = predictions.OrderByDescending(p => p.QuantitySold).First();

                return Json(new
                {
                    success = true,
                    categoryName = topCategory.CategoryName,
                    predictedQty = topCategory.QuantitySold,
                    message = $"Dự đoán 3 tháng tới cần nhập {topCategory.QuantitySold} chiếc {topCategory.CategoryName}."
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Đang chờ huấn luyện mô hình AI..." });
            }
        }

        // Lấy các sản phẩm trong danh mục có doanh thu cao nhất
        public async Task<IActionResult> GetImportSuggestionsForCategory(string categoryName, int aiPredictedQuantity)
        {
            // Lấy danh sách Top 5 sản phẩm bán chạy nhất thuộc danh mục trong 3 tháng qua
            var topProducts = await _cube.GetTopProductsForCategoryAsync(categoryName, 5);
            if (topProducts.Count == 0)
                return Json(new { success = false, message = "Không tìm thấy sản phẩm nổi bật" });

            // Tổng số lượng đã bán để chia tỷ trọng
            int totalHistory = topProducts.Sum(p => p.QuantitySold);
            var importSuggestions = new List<Object>();

            // Phân bổ tỷ lệ đóng góp
            foreach(var prod in topProducts)
            {
                double ratio = (double)prod.QuantitySold / totalHistory;

                // Số lượng đề xuất nhập = Tổng hạn mức dự đoán * % đóng góp
                int suggestImportQty = (int)Math.Round(aiPredictedQuantity * ratio);

                if (suggestImportQty == 0) suggestImportQty = 1;

                importSuggestions.Add(new
                {
                    productId = prod.SourceProductId,
                    productName = prod.ProductName,
                    imageUrl = prod.ImageUrl,
                    historySold = prod.QuantitySold,
                    suggestImport = suggestImportQty
                });
            }

            return Json(new { success = true, data = importSuggestions });
        }

        // Lấy các phiếu nhập kho nhân viên vừa tạo
        [HttpGet]
        public async Task<IActionResult> PendingReceipts()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            // Lấy danh sách phiếu nhập
            var pendings = _db.InventoryReceipts
                .Include(r => r.Supplier)
                .Include(r => r.Creator)
                .Where(r => r.Status == 0)
                .OrderByDescending(r => r.ImportDate)
                .ToListAsync();

            return View(pendings);
        }

        // Duyệt phiếu nhập
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveReceipt(int receiptId)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var receipt = await _db.InventoryReceipts
                .Include(r => r.InventoryReceiptDetails)
                .FirstOrDefaultAsync(r => r.ReceiptID == receiptId && r.Status == 0);

            if (receipt == null)
            {
                TempData["Error"] = "Phiếu nhập không tồn tại hoặc đã được người khác xử lý.";
                return RedirectToAction("PendingReceipts");
            }

            // Mở cổng giao dịch
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                receipt.Status = 1;

                // Quét qua từng mặt hàng trong phiếu nhập
                foreach (var detail in receipt.InventoryReceiptDetails)
                {
                    // Tìm mặt hàng tương ứng trong kho
                    var sizeStock = await _db.ProductSizes.FirstOrDefaultAsync(s => s.SizeID == detail.SizeID);
                    if (sizeStock != null)
                        sizeStock.StockQuantity += detail.Quantity;
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                TempData["Success"] = $"Đã duyệt phiếu số #{receiptId}. Hàng đã chính thức lên kệ!";
            }

            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["Error"] = "Lỗi hệ thống khi cập nhật kho: " + ex.Message;
            }

            return RedirectToAction("PendingReceipts");
        }

        // Bác bỏ phiếu nhập
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectReceipt(int receiptId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var receipt = await _db.InventoryReceipts.FirstOrDefaultAsync(r => r.ReceiptID == receiptId && r.Status == 0);
            if (receipt != null)
            {
                receipt.Status = 2;
                await _db.SaveChangesAsync();

                TempData["Success"] = $"Đã từ chối phiếu nhập số #{receiptId}. Số lượng trong kho không bị thay đổi.";
            }

            return RedirectToAction("PendingReceipts");
        }
    }
}