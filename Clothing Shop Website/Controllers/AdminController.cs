using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Clothing_Shop_Website.Data;
using Clothing_Shop_Website.Models;
using Clothing_Shop_Website.Models.ViewModels;
using Clothing_Shop_Website.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

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

        // ── Kiểm tra quyền Admin ──
        private bool IsAdmin() => HttpContext.Session.GetInt32("Role") == 0 || HttpContext.Session.GetInt32("Role") == 1;

        [HttpGet]
        public async Task<IActionResult> GetImportSuggestionsForCategory(string categoryName, int aiPredictedQuantity)
        {
            // 1. Gọi Cube lấy Top 5 sản phẩm bán chạy nhất của Danh mục này trong 3 tháng qua
            var topProducts = await _cube.GetTopProductsForCategoryAsync(categoryName, 5);

            if (topProducts.Count == 0) return Json(new { success = false, message = "Không tìm thấy sản phẩm nổi bật." });

            // 2. Tính tổng số lượng đã bán của 5 sản phẩm này để lấy Tỷ trọng
            int totalSoldHistory = topProducts.Sum(p => p.QuantitySold);

            var importSuggestions = new List<object>();

            // 3. Phân bổ con số aiPredictedQuantity (Hạn mức) cho từng sản phẩm
            foreach (var prod in topProducts)
            {
                // Tính tỷ lệ % đóng góp của sản phẩm này
                double ratio = (double)prod.QuantitySold / totalSoldHistory;

                // Đề xuất nhập = Hạn mức AI * Tỷ lệ %
                int suggestImportQty = (int)Math.Round(aiPredictedQuantity * ratio);

                // Đảm bảo mỗi món nhập ít nhất 1 cái nếu tỷ lệ quá nhỏ
                if (suggestImportQty == 0) suggestImportQty = 1;

                importSuggestions.Add(new
                {
                    productId = prod.SourceProductId,
                    productName = prod.ProductName,
                    imageUrl = prod.ImageUrl,
                    historySold = prod.QuantitySold,
                    suggestImport = suggestImportQty // Con số vàng dành cho form nhập hàng
                });
            }

            return Json(new { success = true, data = importSuggestions });
        }

        [HttpGet]
        public async Task<IActionResult> GetAIPrediction()
        {
            try
            {
                // Gọi hàm DMX lấy dự báo 3 tháng tới (Hàm GetForecastNext3MonthsAsync bạn đã tạo ở bước trước)
                var predictions = await _cube.GetForecastNext3MonthsAsync();

                if (predictions == null || predictions.Count == 0)
                    return Json(new { success = false, message = "Chưa đủ dữ liệu chuỗi thời gian để dự báo." });

                // Tìm danh mục có xu hướng tăng mạnh nhất (số lượng dự báo cao nhất)
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
            return Json(new
            {
                vm.CubeError,
                kpi = new { vm.TotalRevenue, vm.TotalSalesLines, vm.DistinctCustomers, vm.DistinctProductsSold },
                revenue6m = vm.RevenueLastMonths,
                top = vm.TopSellers,
                catPie = vm.RevenueByCategory,
                ageRev = vm.RevenueByAgeGroup
            });
        }

        [HttpGet]
        public async Task<IActionResult> SearchProducts(string? q)
        {
            if (!IsAdmin()) return Unauthorized();
            q = (q ?? "").Trim();
            if (q.Length < 1) return Json(Array.Empty<object>());
            var list = await _db.Products.AsNoTracking()
                .Where(p => p.ProductName.Contains(q))
                .OrderBy(p => p.ProductName)
                .Take(20)
                .Select(p => new { id = p.ProductID, name = p.ProductName })
                .ToListAsync();
            return Json(list);
        }

        [HttpGet]
        public async Task<IActionResult> SearchSuppliers(string? q)
        {
            if (!IsAdmin()) return Unauthorized();
            q = (q ?? "").Trim();
            if (q.Length < 1) return Json(Array.Empty<object>());
            var list = await _db.Suppliers.AsNoTracking()
                .Where(s => s.SupplierName.Contains(q))
                .OrderBy(s => s.SupplierName)
                .Take(20)
                .Select(s => new { id = s.SupplierID, name = s.SupplierName })
                .ToListAsync();
            return Json(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportStock(
            int supplierId,
            int? productId,
            string productName,
            int categoryId,
            int session,
            decimal salePrice,
            decimal? originalPrice,
            string sizeName,
            int quantity,
            IFormFile? imageFile,
            string? returnPage)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            bool toProducts = string.Equals((returnPage ?? "").Trim(), "Products", StringComparison.OrdinalIgnoreCase);
            IActionResult GoBack() => toProducts ? RedirectToAction("Products") : RedirectToAction("Dashboard");

            if (quantity < 1 || string.IsNullOrWhiteSpace(sizeName))
            {
                TempData["Error"] = "Số lượng và size là bắt buộc.";
                return GoBack();
            }

            if (supplierId < 1 || !await _db.Suppliers.AnyAsync(s => s.SupplierID == supplierId))
            {
                TempData["Error"] = "Vui lòng chọn nhà cung cấp hợp lệ từ gợi ý.";
                return GoBack();
            }

            if (!await _db.Categories.AnyAsync(c => c.CategoryID == categoryId))
            {
                TempData["Error"] = "Danh mục không hợp lệ.";
                return GoBack();
            }

            productName = (productName ?? "").Trim();
            sizeName = sizeName.Trim();
            if (string.IsNullOrEmpty(productName))
            {
                TempData["Error"] = "Tên sản phẩm không được để trống.";
                return GoBack();
            }

            string? imageUrl = null;
            if (imageFile is { Length: > 0 })
            {
                var ext = Path.GetExtension(imageFile.FileName);
                if (ext.Length > 10) ext = "";
                var safe = $"{Guid.NewGuid():N}{ext}";
                var dir = Path.Combine(_env.WebRootPath, "uploads", "products");
                Directory.CreateDirectory(dir);
                var full = Path.Combine(dir, safe);
                await using (var fs = System.IO.File.Create(full))
                    await imageFile.CopyToAsync(fs);
                imageUrl = "/uploads/products/" + safe;
            }

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                Product product;
                if (productId is > 0)
                {
                    product = await _db.Products.Include(p => p.ProductSizes)
                        .FirstAsync(p => p.ProductID == productId.Value);
                    product.ProductName = productName;
                    product.CategoryID = categoryId;
                    product.Session = session;
                    product.Price = salePrice;
                    if (!string.IsNullOrEmpty(imageUrl))
                        product.ImageUrl = imageUrl;
                }
                else
                {
                    product = new Product
                    {
                        ProductName = productName,
                        CategoryID = categoryId,
                        Session = session,
                        Price = salePrice,
                        ImageUrl = imageUrl,
                        Description = ""
                    };
                    _db.Products.Add(product);
                    await _db.SaveChangesAsync();
                }

                var size = product.ProductSizes.FirstOrDefault(s => s.SizeName == sizeName);
                if (size == null)
                {
                    size = new ProductSize
                    {
                        ProductID = product.ProductID,
                        SizeName = sizeName,
                        StockQuantity = quantity,
                        MinimumStock = 0
                    };
                    _db.ProductSizes.Add(size);
                }
                else
                    size.StockQuantity += quantity;

                await _db.SaveChangesAsync();

                var receipt = new InventoryReceipt
                {
                    SupplierID = supplierId,
                    ImportDate = DateTime.Now
                };
                _db.InventoryReceipts.Add(receipt);
                await _db.SaveChangesAsync();

                _db.InventoryReceiptDetails.Add(new InventoryReceiptDetail
                {
                    ReceiptID = receipt.ReceiptID,
                    SizeID = size.SizeID,
                    Quantity = quantity,
                    ImportPrice = originalPrice ?? salePrice
                });

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                TempData["Success"] = toProducts
                    ? "Đã thêm / cập nhật sản phẩm và ghi nhận phiếu nhập."
                    : "Đã ghi nhận nhập hàng.";
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["Error"] = "Lỗi nhập hàng: " + ex.Message;
            }

            return GoBack();
        }

        // ═══════════════════════════════
        //   SẢN PHẨM
        // ═══════════════════════════════
        public async Task<IActionResult> Products(string? search, int? categoryId, string? season)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var query = _db.Products.Include(p => p.Category).AsQueryable();
            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.ProductName.Contains(search));
            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryID == categoryId);
            if (!string.IsNullOrEmpty(season) && int.TryParse(season, out int s))
                query = query.Where(p => p.Session == s);

            ViewBag.Categories = await _db.Categories.ToListAsync();
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.Season = season;

            return View(await query.OrderByDescending(p => p.ProductID).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(
            string productName, int categoryID, int session,
            decimal price,
            int stock, string? imageUrl, string? description)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            _db.Products.Add(new Product
            {
                ProductName = productName,
                CategoryID = categoryID,
                Session = session,
                Price = price,
                ImageUrl = imageUrl,
                Description = description
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã thêm sản phẩm!";
            return RedirectToAction("Products");
        }

        [HttpPost]
        public async Task<IActionResult> EditProduct(
            int productID, string productName, int categoryID, int session,
            decimal price,
            string? imageUrl, string? description)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var p = await _db.Products.FindAsync(productID);
            if (p != null)
            {
                p.ProductName = productName;
                p.CategoryID = categoryID;
                p.Session = session;
                p.Price = price;
                p.ImageUrl = imageUrl;
                p.Description = description;
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã cập nhật sản phẩm!";
            }
            return RedirectToAction("Products");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProduct(int productID)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var p = await _db.Products.FindAsync(productID);
            if (p != null)
            {
                _db.Products.Remove(p);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã xóa sản phẩm!";
            }
            return RedirectToAction("Products");
        }

        // ═══════════════════════════════
        //   ĐƠN HÀNG
        // ═══════════════════════════════
        public async Task<IActionResult> Orders(string? status, string? search)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var query = _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
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
        public async Task<IActionResult> UpdateOrderStatus(int orderId, int status)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var order = await _db.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = status;
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã cập nhật trạng thái!";
            }
            return RedirectToAction("Orders");
        }

        // ═══════════════════════════════
        //   KHÁCH HÀNG
        // ═══════════════════════════════
        public async Task<IActionResult> Customers(string? search)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var query = _db.Users.Include(u => u.CustomerDetail).Where(u => u.Role == 2).AsQueryable();
            if (!string.IsNullOrEmpty(search))
                query = query.Where(u => u.FullName.Contains(search) || u.Phone.Contains(search));

            ViewBag.Search = search;
            return View(await query.OrderByDescending(u => u.UserID).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> ToggleLockUser(int userId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

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
        //   MÃ GIẢM GIÁ
        // ═══════════════════════════════
        public async Task<IActionResult> Discounts()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View(await _db.Discounts.OrderByDescending(d => d.DiscountID).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> AddDiscount(
            string code, decimal discountValue, string discountType,
            int quantity, DateTime expirationDate)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (await _db.Discounts.AnyAsync(d => d.Code == code.ToUpper()))
            {
                TempData["Error"] = "Mã này đã tồn tại!";
                return RedirectToAction("Products");
            }

            int typeInt = string.Equals(discountType, "percent", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

            _db.Discounts.Add(new Discount
            {
                Code = code.ToUpper(),
                DiscountValue = discountValue,
                DiscountType = typeInt,
                Quantity = quantity,
                UsedCount = 0,
                ExpirationDate = expirationDate
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã thêm mã giảm giá!";
            return RedirectToAction("Products");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDiscount(int discountId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var d = await _db.Discounts.FindAsync(discountId);
            if (d != null) { _db.Discounts.Remove(d); await _db.SaveChangesAsync(); }
            TempData["Success"] = "Đã xóa mã!";
            return RedirectToAction("Products");
        }

    }
}