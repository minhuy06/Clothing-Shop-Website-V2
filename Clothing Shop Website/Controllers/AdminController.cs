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
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;

namespace Clothing_Shop_Website.Controllers
{
    // Helper for SetShifts action
    public class ShiftInput
    {
        public int ShiftType { get; set; }
        public string DayOfWeek { get; set; } = "";
    }

    public class ImportReceiptLineInput
    {
        public int ProductId { get; set; }
        public decimal ImportPrice { get; set; }
        public int StockS { get; set; }
        public int StockM { get; set; }
        public int StockL { get; set; }
    }

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
        public async Task<IActionResult> GetAIPrediction(int months = 3)
        {
            if (months < 1) months = 1;
            if (months > 24) months = 24;
            try
            {
                var predictions = await _cube.GetForecastNext3MonthsAsync();

                if (predictions == null || predictions.Count == 0)
                    return Json(new { success = false, message = "Chưa đủ dữ liệu chuỗi thời gian để dự báo." });

                var topCategory = predictions.OrderByDescending(p => p.QuantitySold).First();

                // Scale quantity by chosen months (base model is 3 months)
                int scaledQty = (int)Math.Round(topCategory.QuantitySold * (months / 3.0));
                if (scaledQty < 1) scaledQty = 1;

                return Json(new
                {
                    success = true,
                    categoryName = topCategory.CategoryName,
                    predictedQty = scaledQty,
                    months = months,
                    message = $"Dự đoán {months} tháng tới cần nhập {scaledQty} chiếc {topCategory.CategoryName}."
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

            Clothing_Shop_Website.Models.ViewModels.AdminDashboardViewModel vm;
            try
            {
                vm = await _cube.BuildDashboardAsync(season, ageGroup);
            }
            catch (Exception ex)
            {
                vm = new Clothing_Shop_Website.Models.ViewModels.AdminDashboardViewModel
                {
                    CubeError = "Không kết nối được SSAS cube. Các tính năng AI và thống kê sẽ tạm thời không khả dụng. (" + ex.Message + ")"
                };
            }

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
                        Description = "",
                        Status = 0
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
                TempData["LastReceiptId"] = receipt.ReceiptID;
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
        public async Task<IActionResult> Products(string? search, int? categoryId, string? season, int page = 1, int receiptPage = 1)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            const int pageSize = 10;
            const int receiptPageSize = 10;
            if (page < 1) page = 1;
            if (receiptPage < 1) receiptPage = 1;

            var query = _db.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.ProductName.Contains(search));
            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryID == categoryId);
            if (!string.IsNullOrEmpty(season) && int.TryParse(season, out int s))
                query = query.Where(p => p.Session == s);

            var totalFiltered = await query.CountAsync();
            var totalPages = totalFiltered == 0 ? 1 : (int)Math.Ceiling(totalFiltered / (double)pageSize);
            if (page > totalPages) page = totalPages;

            var products = await query
                .OrderByDescending(p => p.ProductID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Categories = await _db.Categories.AsNoTracking().OrderBy(c => c.CategoryName).ToListAsync();
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.Season = season;
            ViewBag.TotalInDb = await _db.Products.AsNoTracking().CountAsync();
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalFiltered = totalFiltered;
            ViewBag.TotalPages = totalPages;

            var receiptTotal = await _db.InventoryReceipts.AsNoTracking().CountAsync();
            var receiptTotalPages = receiptTotal == 0 ? 1 : (int)Math.Ceiling(receiptTotal / (double)receiptPageSize);
            if (receiptPage > receiptTotalPages) receiptPage = receiptTotalPages;

            ViewBag.Receipts = await _db.InventoryReceipts.AsNoTracking()
                .Include(r => r.Supplier)
                .Include(r => r.InventoryReceiptDetails)
                    .ThenInclude(d => d.ProductSize)
                        .ThenInclude(s => s.Product)
                .OrderByDescending(r => r.ReceiptID)
                .Skip((receiptPage - 1) * receiptPageSize)
                .Take(receiptPageSize)
                .ToListAsync();
            ViewBag.ReceiptPage = receiptPage;
            ViewBag.ReceiptPageSize = receiptPageSize;
            ViewBag.ReceiptTotal = receiptTotal;
            ViewBag.ReceiptTotalPages = receiptTotalPages;
            ViewBag.Suppliers = await _db.Suppliers.AsNoTracking()
                .OrderBy(s => s.SupplierName)
                .ToListAsync();
            ViewBag.Discounts = await _db.Discounts.AsNoTracking()
                .OrderByDescending(d => d.DiscountID)
                .ToListAsync();

            return View(products);
        }

        /// <summary>API: danh sách toàn bộ sản phẩm (admin).</summary>
        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            if (!IsAdmin()) return Unauthorized();

            var list = await _db.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                .OrderByDescending(p => p.ProductID)
                .Select(p => new
                {
                    id = p.ProductID,
                    name = p.ProductName,
                    category = p.Category.CategoryName,
                    categoryId = p.CategoryID,
                    season = p.Session,
                    price = p.Price,
                    imageUrl = p.ImageUrl,
                    description = p.Description,
                    stock = p.ProductSizes.Sum(s => s.StockQuantity),
                    sizes = p.ProductSizes.Select(s => new { s.SizeName, s.StockQuantity }).ToList()
                })
                .ToListAsync();

            return Json(new { success = true, count = list.Count, data = list });
        }

        [HttpGet]
        public async Task<IActionResult> GetProduct(int id)
        {
            if (!IsAdmin()) return Unauthorized();

            var p = await _db.Products.AsNoTracking()
                .Include(x => x.Category)
                .Include(x => x.ProductSizes)
                .FirstOrDefaultAsync(x => x.ProductID == id);

            if (p == null)
                return Json(new { success = false, message = "Không tìm thấy sản phẩm." });

            return Json(new
            {
                success = true,
                product = new
                {
                    id = p.ProductID,
                    name = p.ProductName,
                    categoryId = p.CategoryID,
                    categoryName = p.Category?.CategoryName,
                    season = p.Session,
                    price = p.Price,
                    imageUrl = p.ImageUrl ?? "",
                    description = p.Description ?? "",
                    color = p.Color ?? "",
                    style = p.Style ?? "",
                    material = p.Material ?? "",
                    status = p.Status,
                    stock = p.ProductSizes.Sum(s => s.StockQuantity),
                    sizes = p.ProductSizes
                        .OrderBy(s => s.SizeName)
                        .Select(s => new { id = s.SizeID, sizeName = s.SizeName, stockQuantity = s.StockQuantity })
                        .ToList()
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(
            string productName,
            int categoryID,
            int session,
            decimal price,
            string? imageUrl,
            string? description,
            string? color,
            string? style,
            string? material,
            int stockS,
            int stockM,
            int stockL,
            IFormFile? imageFile)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            productName = (productName ?? "").Trim();
            if (string.IsNullOrEmpty(productName))
            {
                TempData["Error"] = "Tên sản phẩm không được để trống.";
                return RedirectToAction("Products");
            }

            if (price < 0)
            {
                TempData["Error"] = "Giá bán phải >= 0.";
                return RedirectToAction("Products");
            }

            if (stockS < 0 || stockM < 0 || stockL < 0)
            {
                TempData["Error"] = "Tồn kho từng size không được âm.";
                return RedirectToAction("Products");
            }

            if (!await _db.Categories.AnyAsync(c => c.CategoryID == categoryID))
            {
                TempData["Error"] = "Danh mục không hợp lệ.";
                return RedirectToAction("Products");
            }

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

            var product = new Product
            {
                ProductName = productName,
                CategoryID = categoryID,
                Session = session,
                Price = price,
                ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim(),
                Description = description?.Trim(),
                Color = string.IsNullOrWhiteSpace(color) ? null : color.Trim(),
                Style = string.IsNullOrWhiteSpace(style) ? null : style.Trim(),
                Material = string.IsNullOrWhiteSpace(material) ? null : material.Trim(),
                Status = 0
            };
            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            SetProductSizeStock(product, "S", stockS);
            SetProductSizeStock(product, "M", stockM);
            SetProductSizeStock(product, "L", stockL);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đã thêm sản phẩm #" + product.ProductID.ToString("D3") + "!";
            return RedirectToAction("Products");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(
            int productID,
            string productName,
            int categoryID,
            int session,
            decimal price,
            string? imageUrl,
            string? description,
            string? color,
            string? style,
            string? material,
            int stockS,
            int stockM,
            int stockL,
            IFormFile? imageFile)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            productName = (productName ?? "").Trim();
            if (string.IsNullOrEmpty(productName))
            {
                TempData["Error"] = "Tên sản phẩm không được để trống.";
                return RedirectToAction("Products");
            }

            if (price < 0)
            {
                TempData["Error"] = "Giá bán phải >= 0.";
                return RedirectToAction("Products");
            }

            if (!await _db.Categories.AnyAsync(c => c.CategoryID == categoryID))
            {
                TempData["Error"] = "Danh mục không hợp lệ.";
                return RedirectToAction("Products");
            }

            var p = await _db.Products
                .Include(x => x.ProductSizes)
                .FirstOrDefaultAsync(x => x.ProductID == productID);
            if (p == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm.";
                return RedirectToAction("Products");
            }

            if (stockS < 0 || stockM < 0 || stockL < 0)
            {
                TempData["Error"] = "Tồn kho từng size không được âm.";
                return RedirectToAction("Products");
            }

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

            p.ProductName = productName;
            p.CategoryID = categoryID;
            p.Session = session;
            p.Price = price;
            p.ImageUrl = imageFile is { Length: > 0 } ? imageUrl : p.ImageUrl;
            p.Description = description?.Trim();
            p.Color = string.IsNullOrWhiteSpace(color) ? null : color.Trim();
            p.Style = string.IsNullOrWhiteSpace(style) ? null : style.Trim();
            p.Material = string.IsNullOrWhiteSpace(material) ? null : material.Trim();

            SetProductSizeStock(p, "S", stockS);
            SetProductSizeStock(p, "M", stockM);
            SetProductSizeStock(p, "L", stockL);

            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật sản phẩm #" + productID.ToString("D3") + " (tồn S/M/L: " + stockS + "/" + stockM + "/" + stockL + ")!";
            return RedirectToAction("Products");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishProduct(int productId)
        {
            if (!IsAdmin()) return Unauthorized();

            var p = await _db.Products.FindAsync(productId);
            if (p == null)
                return Json(new { success = false, message = "Không tìm thấy sản phẩm." });

            p.Status = p.Status == 1 ? 0 : 1;
            await _db.SaveChangesAsync();
            var msg = p.Status == 1
                ? "Đã cập nhật sản phẩm #" + productId.ToString("D3") + " lên giao diện khách hàng."
                : "Đã ẩn sản phẩm #" + productId.ToString("D3") + " khỏi giao diện khách hàng.";
            return Json(new { success = true, status = p.Status, message = msg });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateImportReceipt(int supplierId, string linesJson)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Không có quyền." });

            if (supplierId < 1 || !await _db.Suppliers.AnyAsync(s => s.SupplierID == supplierId))
                return Json(new { success = false, message = "Vui lòng chọn nhà cung cấp." });

            List<ImportReceiptLineInput> lines;
            try
            {
                lines = JsonSerializer.Deserialize<List<ImportReceiptLineInput>>(linesJson ?? "[]",
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ImportReceiptLineInput>();
            }
            catch
            {
                return Json(new { success = false, message = "Dữ liệu phiếu nhập không hợp lệ." });
            }

            if (lines.Count < 1)
                return Json(new { success = false, message = "Thêm ít nhất một sản phẩm vào phiếu nhập." });

            foreach (var line in lines)
            {
                if (line.ProductId < 1)
                    return Json(new { success = false, message = "Chọn sản phẩm hợp lệ cho từng dòng." });
                if (line.ImportPrice < 0)
                    return Json(new { success = false, message = "Giá gốc không được âm." });
                if (line.StockS < 0 || line.StockM < 0 || line.StockL < 0)
                    return Json(new { success = false, message = "Số lượng size không được âm." });
                if (line.StockS + line.StockM + line.StockL < 1)
                    return Json(new { success = false, message = "Mỗi sản phẩm cần nhập ít nhất 1 size (S/M/L)." });
            }

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var receipt = new InventoryReceipt
                {
                    SupplierID = supplierId,
                    ImportDate = DateTime.Now
                };
                _db.InventoryReceipts.Add(receipt);
                await _db.SaveChangesAsync();

                foreach (var line in lines)
                {
                    var product = await _db.Products.Include(p => p.ProductSizes)
                        .FirstOrDefaultAsync(p => p.ProductID == line.ProductId);
                    if (product == null)
                    {
                        await tx.RollbackAsync();
                        return Json(new { success = false, message = "Không tìm thấy sản phẩm #" + line.ProductId + "." });
                    }

                    foreach (var (sizeName, qty) in new[] { ("S", line.StockS), ("M", line.StockM), ("L", line.StockL) })
                    {
                        if (qty < 1) continue;
                        var size = product.ProductSizes.FirstOrDefault(s =>
                            string.Equals(s.SizeName, sizeName, StringComparison.OrdinalIgnoreCase));
                        if (size == null)
                        {
                            size = new ProductSize
                            {
                                ProductID = product.ProductID,
                                SizeName = sizeName,
                                StockQuantity = qty,
                                MinimumStock = 0
                            };
                            _db.ProductSizes.Add(size);
                            await _db.SaveChangesAsync();
                            product.ProductSizes.Add(size);
                        }
                        else
                            size.StockQuantity += qty;

                        _db.InventoryReceiptDetails.Add(new InventoryReceiptDetail
                        {
                            ReceiptID = receipt.ReceiptID,
                            SizeID = size.SizeID,
                            Quantity = qty,
                            ImportPrice = line.ImportPrice
                        });
                    }
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return Json(new
                {
                    success = true,
                    receiptId = receipt.ReceiptID,
                    message = "Đã tạo phiếu nhập #" + receipt.ReceiptID.ToString("D6") + "."
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return Json(new { success = false, message = "Lỗi tạo phiếu nhập: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetImportReceipt(int id)
        {
            if (!IsAdmin()) return Unauthorized();

            var r = await _db.InventoryReceipts.AsNoTracking()
                .Include(x => x.Supplier)
                .Include(x => x.InventoryReceiptDetails)
                    .ThenInclude(d => d.ProductSize)
                        .ThenInclude(s => s.Product)
                .FirstOrDefaultAsync(x => x.ReceiptID == id);

            if (r == null)
                return Json(new { success = false, message = "Không tìm thấy phiếu nhập." });

            var details = r.InventoryReceiptDetails.OrderBy(d => d.DetailID).ToList();
            var sizeTotals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["S"] = 0, ["M"] = 0, ["L"] = 0 };
            foreach (var d in details)
            {
                var sn = d.ProductSize?.SizeName ?? "";
                if (sizeTotals.ContainsKey(sn))
                    sizeTotals[sn] += d.Quantity;
            }

            var lines = details.Select(d => new
            {
                productName = d.ProductSize?.Product?.ProductName ?? "—",
                sizeName = d.ProductSize?.SizeName ?? "—",
                quantity = d.Quantity,
                importPrice = d.ImportPrice,
                lineTotal = d.Quantity * d.ImportPrice
            }).ToList();

            return Json(new
            {
                success = true,
                receipt = new
                {
                    id = r.ReceiptID,
                    importDate = r.ImportDate.ToString("dd/MM/yyyy HH:mm"),
                    supplierName = r.Supplier?.SupplierName ?? "—",
                    sizeS = sizeTotals["S"],
                    sizeM = sizeTotals["M"],
                    sizeL = sizeTotals["L"],
                    totalAmount = details.Sum(d => d.Quantity * d.ImportPrice),
                    lines
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImportReceipt(int receiptId)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Không có quyền." });

            var receipt = await _db.InventoryReceipts
                .Include(r => r.InventoryReceiptDetails)
                    .ThenInclude(d => d.ProductSize)
                .FirstOrDefaultAsync(r => r.ReceiptID == receiptId);

            if (receipt == null)
                return Json(new { success = false, message = "Không tìm thấy phiếu nhập." });

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                foreach (var d in receipt.InventoryReceiptDetails)
                {
                    if (d.ProductSize != null)
                    {
                        d.ProductSize.StockQuantity -= d.Quantity;
                        if (d.ProductSize.StockQuantity < 0)
                            d.ProductSize.StockQuantity = 0;
                    }
                }
                _db.InventoryReceiptDetails.RemoveRange(receipt.InventoryReceiptDetails);
                _db.InventoryReceipts.Remove(receipt);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                return Json(new { success = true, message = "Đã xóa phiếu nhập #" + receiptId.ToString("D6") + " và trừ tồn kho tương ứng." });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return Json(new { success = false, message = "Lỗi xóa phiếu: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSupplier(int id)
        {
            if (!IsAdmin()) return Unauthorized();

            var s = await _db.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.SupplierID == id);
            if (s == null)
                return Json(new { success = false, message = "Không tìm thấy nhà cung cấp." });

            return Json(new
            {
                success = true,
                supplier = new
                {
                    id = s.SupplierID,
                    name = s.SupplierName,
                    phone = s.Phone ?? "",
                    city = s.City ?? "",
                    country = s.Country ?? ""
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSupplier(int supplierId, string supplierName, string? phone, string? city, string? country)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            supplierName = (supplierName ?? "").Trim();
            if (string.IsNullOrEmpty(supplierName))
            {
                TempData["Error"] = "Tên nhà cung cấp không được để trống.";
                return RedirectToAction("Products");
            }

            var s = await _db.Suppliers.FindAsync(supplierId);
            if (s == null)
            {
                TempData["Error"] = "Không tìm thấy nhà cung cấp.";
                return RedirectToAction("Products");
            }

            if (await _db.Suppliers.AnyAsync(x => x.SupplierName == supplierName && x.SupplierID != supplierId))
            {
                TempData["Error"] = "Tên nhà cung cấp \"" + supplierName + "\" đã được dùng.";
                return RedirectToAction("Products");
            }

            s.SupplierName = supplierName;
            s.Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
            s.City = string.IsNullOrWhiteSpace(city) ? null : city.Trim();
            s.Country = string.IsNullOrWhiteSpace(country) ? null : country.Trim();
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật nhà cung cấp \"" + supplierName + "\".";
            return RedirectToAction("Products");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSupplier(int supplierId)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Không có quyền." });

            var s = await _db.Suppliers.FindAsync(supplierId);
            if (s == null)
                return Json(new { success = false, message = "Không tìm thấy nhà cung cấp." });

            if (await _db.InventoryReceipts.AnyAsync(r => r.SupplierID == supplierId))
                return Json(new { success = false, message = "Không thể xóa — nhà cung cấp đã có phiếu nhập." });

            _db.Suppliers.Remove(s);
            await _db.SaveChangesAsync();
            return Json(new { success = true, message = "Đã xóa nhà cung cấp \"" + s.SupplierName + "\"." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSupplier(string supplierName, string? phone, string? city, string? country)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            supplierName = (supplierName ?? "").Trim();
            if (string.IsNullOrEmpty(supplierName))
            {
                TempData["Error"] = "Tên nhà cung cấp không được để trống.";
                return RedirectToAction("Products");
            }

            if (await _db.Suppliers.AnyAsync(s => s.SupplierName == supplierName))
            {
                TempData["Error"] = "Nhà cung cấp \"" + supplierName + "\" đã tồn tại.";
                return RedirectToAction("Products");
            }

            _db.Suppliers.Add(new Supplier
            {
                SupplierName = supplierName,
                Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
                City = string.IsNullOrWhiteSpace(city) ? null : city.Trim(),
                Country = string.IsNullOrWhiteSpace(country) ? null : country.Trim()
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã thêm nhà cung cấp \"" + supplierName + "\".";
            return RedirectToAction("Products");
        }

        static void SetProductSizeStock(Product product, string sizeName, int quantity)
        {
            var size = product.ProductSizes.FirstOrDefault(s =>
                string.Equals(s.SizeName, sizeName, StringComparison.OrdinalIgnoreCase));
            if (size == null)
            {
                product.ProductSizes.Add(new ProductSize
                {
                    ProductID = product.ProductID,
                    SizeName = sizeName,
                    StockQuantity = quantity,
                    MinimumStock = 5
                });
            }
            else
                size.StockQuantity = quantity;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int productID)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var p = await _db.Products.FindAsync(productID);
            if (p == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm.";
                return RedirectToAction("Products");
            }

            if (p.Status == 1)
            {
                TempData["Error"] = "Không thể xóa sản phẩm đang hiển thị trên giao diện. Hãy ẩn sản phẩm trước.";
                return RedirectToAction("Products");
            }

            _db.Products.Remove(p);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã xóa sản phẩm!";
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDiscount(
            string code, decimal discountValue, string discountType,
            int quantity, DateTime expirationDate)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            code = (code ?? "").Trim().ToUpper();
            if (string.IsNullOrEmpty(code))
            {
                TempData["Error"] = "Mã giảm giá không được để trống.";
                return Redirect("/Admin/Products#discounts");
            }
            if (discountValue <= 0)
            {
                TempData["Error"] = "Giá trị giảm phải lớn hơn 0.";
                return Redirect("/Admin/Products#discounts");
            }
            if (quantity < 1)
            {
                TempData["Error"] = "Số lượng mã phải ít nhất 1.";
                return Redirect("/Admin/Products#discounts");
            }

            if (await _db.Discounts.AnyAsync(d => d.Code == code))
            {
                TempData["Error"] = "Mã \"" + code + "\" đã tồn tại.";
                return Redirect("/Admin/Products#discounts");
            }

            int typeInt = string.Equals(discountType, "percent", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            if (typeInt == 1 && discountValue > 100)
            {
                TempData["Error"] = "Giảm theo % không được vượt quá 100.";
                return Redirect("/Admin/Products#discounts");
            }

            _db.Discounts.Add(new Discount
            {
                Code = code,
                DiscountValue = discountValue,
                DiscountType = typeInt,
                Quantity = quantity,
                UsedCount = 0,
                ExpirationDate = expirationDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59)
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã thêm mã giảm giá \"" + code + "\".";
            return Redirect("/Admin/Products#discounts");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDiscount(int discountId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var d = await _db.Discounts.FindAsync(discountId);
            if (d == null)
            {
                TempData["Error"] = "Không tìm thấy mã giảm giá.";
                return Redirect("/Admin/Products#discounts");
            }

            if (await _db.Orders.AnyAsync(o => o.DiscountID == discountId))
            {
                TempData["Error"] = "Không thể xóa mã \"" + d.Code + "\" — đã được dùng trong đơn hàng.";
                return Redirect("/Admin/Products#discounts");
            }

            _db.Discounts.Remove(d);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã xóa mã \"" + d.Code + "\".";
            return Redirect("/Admin/Products#discounts");
        }

        // ═══════════════════════════════
        //   NHÂN VIÊN (STAFF MANAGEMENT)
        // ═══════════════════════════════
        public async Task<IActionResult> Staff()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var staff = await _db.Users
                .Include(u => u.StaffDetail)
                    .ThenInclude(sd => sd!.StaffShifts)
                .Where(u => u.Role == 1)
                .OrderByDescending(u => u.UserID)
                .ToListAsync();
            return View(staff);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStaff(string fullName, string phone, string password,
            DateTime hireDate, decimal salary)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (await _db.Users.AnyAsync(u => u.Phone == phone))
            {
                TempData["Error"] = "Số điện thoại đã tồn tại!";
                return RedirectToAction("Staff");
            }

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var user = new User
                {
                    FullName = fullName.Trim(),
                    Phone = phone.Trim(),
                    Password = password,
                    Role = 1,
                    Status = 1,
                    Gender = 0
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                _db.StaffDetails.Add(new StaffDetail
                {
                    UserID = user.UserID,
                    HireDate = hireDate,
                    Salary = salary
                });
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                TempData["Success"] = "Đã thêm nhân viên!";
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["Error"] = "Lỗi: " + ex.Message;
            }
            return RedirectToAction("Staff");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStaff(int userId, string fullName, string phone,
            string? password, DateTime hireDate, decimal salary)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var user = await _db.Users.Include(u => u.StaffDetail)
                .FirstOrDefaultAsync(u => u.UserID == userId && u.Role == 1);
            if (user == null) { TempData["Error"] = "Không tìm thấy nhân viên!"; return RedirectToAction("Staff"); }

            user.FullName = fullName.Trim();
            user.Phone = phone.Trim();
            if (!string.IsNullOrWhiteSpace(password)) user.Password = password;

            if (user.StaffDetail == null)
            {
                _db.StaffDetails.Add(new StaffDetail { UserID = userId, HireDate = hireDate, Salary = salary });
            }
            else
            {
                user.StaffDetail.HireDate = hireDate;
                user.StaffDetail.Salary = salary;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật nhân viên!";
            return RedirectToAction("Staff");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStaff(int userId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == userId && u.Role == 1);
            if (user != null)
            {
                _db.Users.Remove(user);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã xóa nhân viên!";
            }
            return RedirectToAction("Staff");
        }

        [HttpGet]
        public async Task<IActionResult> GetStaffShifts(int userId)
        {
            if (!IsAdmin()) return Unauthorized();
            var shifts = await _db.StaffShifts.Where(s => s.UserID == userId)
                .Select(s => new { s.ShiftID, s.UserID, s.ShiftType, s.DayOfWeek })
                .ToListAsync();
            return Json(shifts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetShifts([FromBody] SetShiftsRequest req)
        {
            if (!IsAdmin()) return Unauthorized();

            var existing = _db.StaffShifts.Where(s => s.UserID == req.UserId);
            _db.StaffShifts.RemoveRange(existing);

            foreach (var s in req.Shifts ?? new List<ShiftInput>())
            {
                _db.StaffShifts.Add(new StaffShift
                {
                    UserID = req.UserId,
                    ShiftType = s.ShiftType,
                    DayOfWeek = s.DayOfWeek
                });
            }
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ═══════════════════════════════
        //   QUẢNG CÁO
        // ═══════════════════════════════
        public async Task<IActionResult> Advertisements()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var ads = await _db.Advertisements.OrderByDescending(a => a.CreatedDate).ToListAsync();
            return View(ads);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAdvertisement(string title, string? linkUrl,
            string position, IFormFile? imageFile)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            string? imageUrl = null;
            if (imageFile is { Length: > 0 })
            {
                var ext = Path.GetExtension(imageFile.FileName);
                if (ext.Length > 10) ext = "";
                var safe = $"{Guid.NewGuid():N}{ext}";
                var dir = Path.Combine(_env.WebRootPath, "uploads", "ads");
                Directory.CreateDirectory(dir);
                var full = Path.Combine(dir, safe);
                await using (var fs = System.IO.File.Create(full)) await imageFile.CopyToAsync(fs);
                imageUrl = "/uploads/ads/" + safe;
            }

            _db.Advertisements.Add(new Advertisement
            {
                Title = (title ?? "").Trim(),
                ImageUrl = imageUrl,
                LinkUrl = linkUrl?.Trim(),
                Position = position ?? "banner",
                IsActive = true,
                CreatedDate = DateTime.Now
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã thêm quảng cáo!";
            return RedirectToAction("Advertisements");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAdvertisement(int adId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var ad = await _db.Advertisements.FindAsync(adId);
            if (ad != null) { _db.Advertisements.Remove(ad); await _db.SaveChangesAsync(); }
            TempData["Success"] = "Đã xóa quảng cáo!";
            return RedirectToAction("Advertisements");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAdActive(int adId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var ad = await _db.Advertisements.FindAsync(adId);
            if (ad != null) { ad.IsActive = !ad.IsActive; await _db.SaveChangesAsync(); }
            return RedirectToAction("Advertisements");
        }

        // ═══════════════════════════════
        //   XUẤT HÓA ĐƠN NHẬP HÀNG
        // ═══════════════════════════════
        [HttpGet]
        public async Task<IActionResult> ExportReceipt(int receiptId, string? format)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var receipt = await _db.InventoryReceipts
                .Include(r => r.Supplier)
                .Include(r => r.InventoryReceiptDetails)
                    .ThenInclude(d => d.ProductSize)
                        .ThenInclude(s => s.Product)
                .FirstOrDefaultAsync(r => r.ReceiptID == receiptId);

            if (receipt == null)
            {
                TempData["Error"] = "Không tìm thấy phiếu nhập!";
                return RedirectToAction("Products");
            }

            if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            {
                var sb = new StringBuilder();
                sb.AppendLine("HÓA ĐƠN NHẬP HÀNG - NEVA");
                sb.AppendLine($"Số phiếu,#{receiptId:D6}");
                sb.AppendLine($"Nhà cung cấp,{receipt.Supplier?.SupplierName}");
                sb.AppendLine($"Ngày nhập,{receipt.ImportDate:dd/MM/yyyy HH:mm}");
                sb.AppendLine();
                sb.AppendLine("STT,Sản phẩm,Size,Số lượng,Giá nhập (đ),Thành tiền (đ)");
                int i = 1;
                decimal total = 0;
                foreach (var d in receipt.InventoryReceiptDetails)
                {
                    decimal sub = d.Quantity * d.ImportPrice;
                    total += sub;
                    var pname = d.ProductSize?.Product?.ProductName ?? "";
                    var sname = d.ProductSize?.SizeName ?? "";
                    sb.AppendLine($"{i++},\"{pname}\",{sname},{d.Quantity},{d.ImportPrice:N0},{sub:N0}");
                }
                sb.AppendLine($",,,, Tổng cộng,{total:N0}");
                var bytes = Encoding.UTF8.GetBytes(sb.ToString());
                return File(bytes, "text/csv; charset=utf-8",
                    $"HoaDonNhap_{receiptId:D6}_{receipt.ImportDate:yyyyMMdd}.csv");
            }

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("PhieuNhap");
            var details = receipt.InventoryReceiptDetails
                .OrderBy(d => d.DetailID)
                .ToList();
            var supplierName = receipt.Supplier?.SupplierName ?? "";
            var red = XLColor.FromHtml("#CC0000");
            var thin = XLBorderStyleValues.Thin;

            void StyleRedBorder(IXLRange range)
            {
                range.Style.Font.FontColor = red;
                range.Style.Border.OutsideBorder = thin;
                range.Style.Border.InsideBorder = thin;
                range.Style.Border.OutsideBorderColor = red;
                range.Style.Border.InsideBorderColor = red;
            }

            ws.Column(1).Width = 6;
            ws.Column(2).Width = 14;
            ws.Column(3).Width = 38;
            ws.Column(4).Width = 8;
            ws.Column(5).Width = 11;
            ws.Column(6).Width = 11;
            ws.Column(7).Width = 16;

            ws.Cell(1, 1).Value = "NEVA";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 16;
            ws.Cell(1, 1).Style.Font.FontColor = red;
            ws.Range(1, 1, 2, 2).Merge();

            ws.Cell(1, 3).Value = "PHIẾU NHẬP KHO";
            ws.Cell(2, 3).Value = "RECEIVING SLIP";
            ws.Range(1, 3, 2, 5).Merge();
            ws.Cell(1, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(1, 3).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Cell(1, 3).Style.Font.Bold = true;
            ws.Cell(1, 3).Style.Font.FontSize = 14;
            ws.Cell(2, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(1, 6).Value = "Số:";
            ws.Cell(1, 7).Value = receiptId.ToString("D6");
            ws.Cell(2, 6).Value = "Ngày/Date:";
            ws.Cell(2, 7).Value = receipt.ImportDate.ToString("dd/MM/yyyy");
            StyleRedBorder(ws.Range(1, 1, 2, 7));

            ws.Cell(3, 1).Value = "Người giao/Deliver:";
            ws.Cell(3, 2).Value = supplierName;
            ws.Range(3, 2, 3, 4).Merge();
            ws.Cell(3, 5).Value = "Bộ phận/Department:";
            ws.Cell(3, 6).Value = receipt.Supplier?.City ?? "";
            ws.Range(3, 6, 3, 7).Merge();
            StyleRedBorder(ws.Range(3, 1, 3, 7));

            ws.Cell(4, 1).Value = "Nội dung/Remarks:";
            ws.Cell(4, 2).Value = "nhập hàng";
            ws.Range(4, 2, 4, 7).Merge();
            StyleRedBorder(ws.Range(4, 1, 4, 7));

            ws.Cell(5, 1).Value = "I- Thành phẩm/Details:";
            ws.Range(5, 1, 5, 7).Merge();
            ws.Cell(5, 1).Style.Font.Bold = true;
            ws.Cell(5, 1).Style.Font.FontColor = red;

            ws.Cell(6, 1).Value = "STT\r\nNo";
            ws.Cell(6, 2).Value = "Mã VT\r\nCode";
            ws.Cell(6, 3).Value = "Tên chi tiết\r\nName & Specification";
            ws.Cell(6, 4).Value = "Đv\r\nUnit";
            ws.Cell(6, 5).Value = "Số lượng/Qty";
            ws.Range(6, 5, 6, 6).Merge();
            ws.Cell(6, 7).Value = "Ghi chú\r\nRemark";
            ws.Cell(7, 5).Value = "Ctừ\r\non Doc.";
            ws.Cell(7, 6).Value = "Thực tế\r\nPhysical";
            ws.Range(6, 1, 6, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Range(6, 1, 7, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(6, 1, 7, 7).Style.Alignment.WrapText = true;
            ws.Range(6, 1, 7, 7).Style.Font.Bold = true;
            StyleRedBorder(ws.Range(6, 1, 7, 7));

            int row = 8;
            int idx = 1;
            int totalQty = 0;
            decimal totalAmt = 0;
            foreach (var d in details)
            {
                var pname = d.ProductSize?.Product?.ProductName ?? "";
                var sname = d.ProductSize?.SizeName ?? "";
                var pid = d.ProductSize?.ProductID ?? 0;
                var lineTotal = d.Quantity * d.ImportPrice;
                totalQty += d.Quantity;
                totalAmt += lineTotal;

                ws.Cell(row, 1).Value = idx++;
                ws.Cell(row, 2).Value = pid > 0 ? pid.ToString("D3") : "";
                ws.Cell(row, 3).Value = string.IsNullOrEmpty(sname) ? pname : pname + " — Size " + sname;
                ws.Cell(row, 4).Value = "Cái";
                ws.Cell(row, 5).Value = d.Quantity;
                ws.Cell(row, 6).Value = d.Quantity;
                ws.Cell(row, 7).Value = d.ImportPrice.ToString("N0") + "đ";
                ws.Range(row, 1, row, 7).Style.Alignment.WrapText = true;
                StyleRedBorder(ws.Range(row, 1, row, 7));
                row++;
            }

            ws.Cell(row, 1).Value = "Tổng cộng";
            ws.Cell(row, 2).Value = "Total";
            ws.Range(row, 1, row, 4).Merge();
            ws.Cell(row, 5).Value = totalQty;
            ws.Cell(row, 6).Value = totalQty;
            ws.Cell(row, 7).Value = totalAmt.ToString("N0") + "đ";
            ws.Range(row, 1, row, 7).Style.Font.Bold = true;
            StyleRedBorder(ws.Range(row, 1, row, 7));
            row += 2;

            ws.Cell(row, 1).Value = "Người giao/Deliver";
            ws.Range(row, 1, row, 3).Merge();
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row + 1, 1).Value = supplierName;
            ws.Range(row + 1, 1, row + 2, 3).Merge();
            ws.Range(row, 1, row + 2, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(row, 1, row + 2, 3).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            StyleRedBorder(ws.Range(row, 1, row + 2, 3));

            ws.Range(1, 1, row + 2, 7).Style.Font.FontColor = red;

            await using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"PhieuNhap_{receiptId:D6}_{receipt.ImportDate:yyyyMMdd}.xlsx");
        }

    }

    // ViewModel cho SetShifts
    public class SetShiftsRequest
    {
        public int UserId { get; set; }
        public List<ShiftInput>? Shifts { get; set; }
    }
}