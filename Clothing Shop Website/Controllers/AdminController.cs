using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using Clothing_Shop_Website.Data;
using Clothing_Shop_Website.Models;
using Clothing_Shop_Website.Enums;
using Clothing_Shop_Website.Helper;
using Clothing_Shop_Website.Services;

namespace Clothing_Shop_Website.Controllers
{
    // Dữ liệu đầu vào
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

        // EXTENSION / HELPER METHOD
        private bool IsAdmin() => HttpContext.Session.GetInt32("Role") == (int)UserRole.Admin;

        #region 1. DASHBOARD & AI

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
        public async Task<IActionResult> GetAIPrediction(int months = 3)
        {
            if (months < 1) months = 1;
            if (months > 24) months = 24;
            try
            {
                var predictions = await _cube.GetForecastNext3MonthsAsync();

                if (predictions == null || predictions.Count == 0)
                    return Json(new { success = false, message = "Chưa đủ dữ liệu chuỗi thời gian để dự báo" });

                var topCategory = predictions.OrderByDescending(p => p.QuantitySold).First();

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

        public async Task<IActionResult> GetImportSuggestionsForCategory(string categoryName, int aiPredictedQuantity)
        {
            var topProducts = await _cube.GetTopProductsForCategoryAsync(categoryName, 5);
            if (topProducts.Count == 0)
                return Json(new { success = false, message = "Không tìm thấy sản phẩm nổi bật" });

            int totalHistory = topProducts.Sum(p => p.QuantitySold);
            var importSuggestions = new List<Object>();

            foreach (var prod in topProducts)
            {
                double ratio = (double)prod.QuantitySold / totalHistory;
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
        #endregion

        #region 2. QUẢN LÝ NHÂN SỰ

        public async Task<IActionResult> StaffMembers(string? search)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var query = _db.Users
                .Include(u => u.StaffDetail)
                    .ThenInclude(sd => sd.StaffShifts)
                .Where(u => u.Role == (int)UserRole.Staff) // ENUM
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(u => u.FullName.Contains(search) || u.Phone.Contains(search));

            return View(await query.OrderByDescending(u => u.UserID).ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStaff(string fullName, string phone, string password, int gender, DateTime dateOfBirth, decimal salary, DateTime hireDate, List<string> selectedShifts)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (await _db.Users.AnyAsync(u => u.Phone == phone))
            {
                TempData["Error"] = "Số điện thoại này đã được sử dụng!";
                return RedirectToAction("StaffMembers");
            }

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var newStaff = new User
                {
                    FullName = fullName.Trim(),
                    Phone = phone.Trim(),
                    Password = SecurityHelper.HashPassword(password),
                    Role = (int)UserRole.Staff,
                    Status = (int)UserStatus.Active,
                    Gender = gender,
                    DateOfBirth = dateOfBirth
                };

                _db.Users.Add(newStaff);
                await _db.SaveChangesAsync();

                _db.StaffDetails.Add(new StaffDetail { UserID = newStaff.UserID, Salary = salary, HireDate = hireDate });
                await _db.SaveChangesAsync();

                if (selectedShifts != null && selectedShifts.Any())
                {
                    foreach (var shiftCode in selectedShifts)
                    {
                        var parts = shiftCode.Split('_');
                        if (parts.Length == 2)
                        {
                            string dayOfWeek = parts[0];
                            if (int.TryParse(parts[1], out int shiftType))
                            {
                                _db.StaffShifts.Add(new StaffShift
                                {
                                    UserID = newStaff.UserID,
                                    DayOfWeek = dayOfWeek,
                                    ShiftType = shiftType
                                });
                            }
                        }
                    }
                }
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                TempData["Success"] = "Đã tuyển dụng nhân viên mới với đầy đủ hồ sơ!";
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["Error"] = "Lỗi khi thêm nhân viên: " + ex.Message;
            }

            return RedirectToAction("StaffMembers");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStaff(int userId, string fullName, string phone, int gender, DateTime dateOfBirth, decimal salary, DateTime hireDate, int status, List<string> selectedShifts)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var user = await _db.Users
                .Include(u => u.StaffDetail)
                    .ThenInclude(sd => sd.StaffShifts)
                .FirstOrDefaultAsync(u => u.UserID == userId && u.Role == (int)UserRole.Staff);

            if (user == null) return NotFound();

            if (await _db.Users.AnyAsync(u => u.Phone == phone && u.UserID != userId))
            {
                TempData["Error"] = "Thất bại: Số điện thoại bị trùng với người khác!";
                return RedirectToAction("StaffMembers");
            }

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                user.FullName = fullName.Trim();
                user.Phone = phone.Trim();
                user.Gender = gender;
                user.DateOfBirth = dateOfBirth;
                user.Status = status;

                if (user.StaffDetail != null) user.StaffDetail.Salary = salary;

                if (user.StaffDetail != null && user.StaffDetail.StaffShifts.Any())
                    _db.StaffShifts.RemoveRange(user.StaffDetail.StaffShifts);

                if (selectedShifts != null && selectedShifts.Any())
                {
                    foreach (var shiftCode in selectedShifts)
                    {
                        var parts = shiftCode.Split('_');
                        if (parts.Length == 2)
                        {
                            string dayOfWeek = parts[0];
                            if (int.TryParse(parts[1], out int shiftType))
                            {
                                _db.StaffShifts.Add(new StaffShift
                                {
                                    UserID = userId,
                                    DayOfWeek = dayOfWeek,
                                    ShiftType = shiftType
                                });
                            }
                        }
                    }
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                TempData["Success"] = "Đã cập nhật hồ sơ và Lưới lịch trực của nhân viên!";
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["Error"] = "Lỗi khi cập nhật dữ liệu: " + ex.Message;
            }

            return RedirectToAction("StaffMembers");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStaff(int userId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var user = await _db.Users
                .Include(u => u.StaffDetail)
                    .ThenInclude(sd => sd.StaffShifts)
                .FirstOrDefaultAsync(u => u.UserID == userId && u.Role == (int)UserRole.Staff);

            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy nhân viên!";
                return RedirectToAction("StaffMembers");
            }

            user.Status = (int)UserStatus.Inactive; // Enum & Soft Delete

            if (user.StaffDetail != null && user.StaffDetail.StaffShifts.Any())
                _db.StaffShifts.RemoveRange(user.StaffDetail.StaffShifts);

            await _db.SaveChangesAsync();

            TempData["Success"] = $"Đã chuyển nhân viên {user.FullName} sang trạng thái Nghỉ việc (Bảo lưu dữ liệu lịch sử thành công).";
            return RedirectToAction("StaffMembers");
        }
        #endregion

        #region 3. QUẢN LÝ SẢN PHẨM (Feature)

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
            ViewBag.Suppliers = await _db.Suppliers.AsNoTracking().OrderBy(s => s.SupplierName).ToListAsync();
            ViewBag.Discounts = await _db.Discounts.AsNoTracking().OrderByDescending(d => d.DiscountID).ToListAsync();

            return View(products);
        }

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

            if (p == null) return Json(new { success = false, message = "Không tìm thấy sản phẩm." });

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
                    sizes = p.ProductSizes.OrderBy(s => s.SizeName).Select(s => new { id = s.SizeID, sizeName = s.SizeName, stockQuantity = s.StockQuantity }).ToList()
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(string productName, int categoryID, int session, decimal price, string? imageUrl, string? description, string? color, string? style, string? material, int stockS, int stockM, int stockL, IFormFile? imageFile)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            productName = (productName ?? "").Trim();
            if (string.IsNullOrEmpty(productName)) { TempData["Error"] = "Tên sản phẩm không được để trống."; return RedirectToAction("Products"); }
            if (price < 0) { TempData["Error"] = "Giá bán phải >= 0."; return RedirectToAction("Products"); }
            if (stockS < 0 || stockM < 0 || stockL < 0) { TempData["Error"] = "Tồn kho không được âm."; return RedirectToAction("Products"); }
            if (!await _db.Categories.AnyAsync(c => c.CategoryID == categoryID)) { TempData["Error"] = "Danh mục không hợp lệ."; return RedirectToAction("Products"); }

            // SỬ DỤNG HELPER UPLOAD ẢNH
            var uploadedUrl = await FileHelper.UploadImageAsync(imageFile, "products", _env);
            if (uploadedUrl != null) imageUrl = uploadedUrl;

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
                Status = (int)ProductStatus.Hidden // ENUM
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
        public async Task<IActionResult> EditProduct(int productID, string productName, int categoryID, int session, decimal price, string? imageUrl, string? description, string? color, string? style, string? material, int stockS, int stockM, int stockL, IFormFile? imageFile)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            productName = (productName ?? "").Trim();
            if (string.IsNullOrEmpty(productName)) { TempData["Error"] = "Tên không được trống."; return RedirectToAction("Products"); }
            if (price < 0) { TempData["Error"] = "Giá bán phải >= 0."; return RedirectToAction("Products"); }
            if (!await _db.Categories.AnyAsync(c => c.CategoryID == categoryID)) { TempData["Error"] = "Danh mục không hợp lệ."; return RedirectToAction("Products"); }
            if (stockS < 0 || stockM < 0 || stockL < 0) { TempData["Error"] = "Tồn kho không âm."; return RedirectToAction("Products"); }

            var p = await _db.Products.Include(x => x.ProductSizes).FirstOrDefaultAsync(x => x.ProductID == productID);
            if (p == null) { TempData["Error"] = "Không tìm thấy sản phẩm."; return RedirectToAction("Products"); }

            // SỬ DỤNG HELPER UPLOAD ẢNH
            var uploadedUrl = await FileHelper.UploadImageAsync(imageFile, "products", _env);
            if (uploadedUrl != null) imageUrl = uploadedUrl;

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
            TempData["Success"] = "Đã cập nhật sản phẩm #" + productID.ToString("D3") + "!";
            return RedirectToAction("Products");
        }

        static void SetProductSizeStock(Product product, string sizeName, int quantity)
        {
            var size = product.ProductSizes.FirstOrDefault(s => string.Equals(s.SizeName, sizeName, StringComparison.OrdinalIgnoreCase));
            if (size == null)
            {
                product.ProductSizes.Add(new ProductSize { ProductID = product.ProductID, SizeName = sizeName, StockQuantity = quantity, MinimumStock = 5 });
            }
            else size.StockQuantity = quantity;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishProduct(int productId)
        {
            if (!IsAdmin()) return Unauthorized();

            var p = await _db.Products.FindAsync(productId);
            if (p == null) return Json(new { success = false, message = "Không tìm thấy sản phẩm." });

            p.Status = p.Status == (int)ProductStatus.Published ? (int)ProductStatus.Hidden : (int)ProductStatus.Published; // 💡 ENUM
            await _db.SaveChangesAsync();
            var msg = p.Status == (int)ProductStatus.Published ? "Đã cập nhật sản phẩm lên giao diện." : "Đã ẩn sản phẩm.";
            return Json(new { success = true, status = p.Status, message = msg });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int productID)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var p = await _db.Products.FindAsync(productID);
            if (p == null) { TempData["Error"] = "Không tìm thấy sản phẩm."; return RedirectToAction("Products"); }

            if (p.Status == (int)ProductStatus.Published) { TempData["Error"] = "Không thể xóa sản phẩm đang hiển thị."; return RedirectToAction("Products"); }

            _db.Products.Remove(p);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã xóa sản phẩm!";
            return RedirectToAction("Products");
        }

        #endregion

        #region 4. QUẢN LÝ PHIẾU NHẬP & NHÀ CUNG CẤP (Feature)

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
                lines = JsonSerializer.Deserialize<List<ImportReceiptLineInput>>(linesJson ?? "[]", new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ImportReceiptLineInput>();
            }
            catch { return Json(new { success = false, message = "Dữ liệu phiếu nhập không hợp lệ." }); }

            if (lines.Count < 1) return Json(new { success = false, message = "Thêm ít nhất một sản phẩm." });

            foreach (var line in lines)
            {
                if (line.ProductId < 1) return Json(new { success = false, message = "Chọn sản phẩm hợp lệ." });
                if (line.ImportPrice < 0) return Json(new { success = false, message = "Giá gốc không được âm." });
                if (line.StockS < 0 || line.StockM < 0 || line.StockL < 0) return Json(new { success = false, message = "Số lượng không được âm." });
                if (line.StockS + line.StockM + line.StockL < 1) return Json(new { success = false, message = "Nhập ít nhất 1 size." });
            }

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var receipt = new InventoryReceipt { SupplierID = supplierId, ImportDate = DateTime.Now };
                _db.InventoryReceipts.Add(receipt);
                await _db.SaveChangesAsync();

                foreach (var line in lines)
                {
                    var product = await _db.Products.Include(p => p.ProductSizes).FirstOrDefaultAsync(p => p.ProductID == line.ProductId);
                    if (product == null) { await tx.RollbackAsync(); return Json(new { success = false, message = "Không tìm thấy sản phẩm." }); }

                    foreach (var (sizeName, qty) in new[] { ("S", line.StockS), ("M", line.StockM), ("L", line.StockL) })
                    {
                        if (qty < 1) continue;
                        var size = product.ProductSizes.FirstOrDefault(s => string.Equals(s.SizeName, sizeName, StringComparison.OrdinalIgnoreCase));
                        if (size == null)
                        {
                            size = new ProductSize { ProductID = product.ProductID, SizeName = sizeName, StockQuantity = qty, MinimumStock = 0 };
                            _db.ProductSizes.Add(size);
                            await _db.SaveChangesAsync();
                            product.ProductSizes.Add(size);
                        }
                        else size.StockQuantity += qty;

                        _db.InventoryReceiptDetails.Add(new InventoryReceiptDetail { ReceiptID = receipt.ReceiptID, SizeID = size.SizeID, Quantity = qty, ImportPrice = line.ImportPrice });
                    }
                }
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                return Json(new { success = true, receiptId = receipt.ReceiptID, message = "Đã tạo phiếu nhập!" });
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

            var r = await _db.InventoryReceipts.AsNoTracking().Include(x => x.Supplier).Include(x => x.InventoryReceiptDetails).ThenInclude(d => d.ProductSize).ThenInclude(s => s.Product).FirstOrDefaultAsync(x => x.ReceiptID == id);

            if (r == null) return Json(new { success = false, message = "Không tìm thấy phiếu nhập." });

            var details = r.InventoryReceiptDetails.OrderBy(d => d.DetailID).ToList();
            var sizeTotals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["S"] = 0, ["M"] = 0, ["L"] = 0 };
            foreach (var d in details)
            {
                var sn = d.ProductSize?.SizeName ?? "";
                if (sizeTotals.ContainsKey(sn)) sizeTotals[sn] += d.Quantity;
            }

            var lines = details.Select(d => new { productName = d.ProductSize?.Product?.ProductName ?? "—", sizeName = d.ProductSize?.SizeName ?? "—", quantity = d.Quantity, importPrice = d.ImportPrice, lineTotal = d.Quantity * d.ImportPrice }).ToList();

            return Json(new { success = true, receipt = new { id = r.ReceiptID, importDate = r.ImportDate.ToString("dd/MM/yyyy HH:mm"), supplierName = r.Supplier?.SupplierName ?? "—", sizeS = sizeTotals["S"], sizeM = sizeTotals["M"], sizeL = sizeTotals["L"], totalAmount = details.Sum(d => d.Quantity * d.ImportPrice), lines } });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImportReceipt(int receiptId)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Không có quyền." });

            var receipt = await _db.InventoryReceipts.Include(r => r.InventoryReceiptDetails).ThenInclude(d => d.ProductSize).FirstOrDefaultAsync(r => r.ReceiptID == receiptId);
            if (receipt == null) return Json(new { success = false, message = "Không tìm thấy phiếu nhập." });

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                foreach (var d in receipt.InventoryReceiptDetails)
                {
                    if (d.ProductSize != null) { d.ProductSize.StockQuantity -= d.Quantity; if (d.ProductSize.StockQuantity < 0) d.ProductSize.StockQuantity = 0; }
                }
                _db.InventoryReceiptDetails.RemoveRange(receipt.InventoryReceiptDetails);
                _db.InventoryReceipts.Remove(receipt);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                return Json(new { success = true, message = "Đã xóa phiếu nhập và trừ tồn kho." });
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
            if (s == null) return Json(new { success = false, message = "Không tìm thấy nhà cung cấp." });
            return Json(new { success = true, supplier = new { id = s.SupplierID, name = s.SupplierName, phone = s.Phone ?? "", city = s.City ?? "", country = s.Country ?? "" } });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSupplier(int supplierId, string supplierName, string? phone, string? city, string? country)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            supplierName = (supplierName ?? "").Trim();
            if (string.IsNullOrEmpty(supplierName)) { TempData["Error"] = "Tên không được trống."; return RedirectToAction("Products"); }

            var s = await _db.Suppliers.FindAsync(supplierId);
            if (s == null) { TempData["Error"] = "Không tìm thấy."; return RedirectToAction("Products"); }
            if (await _db.Suppliers.AnyAsync(x => x.SupplierName == supplierName && x.SupplierID != supplierId)) { TempData["Error"] = "Tên đã tồn tại."; return RedirectToAction("Products"); }

            s.SupplierName = supplierName; s.Phone = phone?.Trim(); s.City = city?.Trim(); s.Country = country?.Trim();
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật nhà cung cấp!";
            return RedirectToAction("Products");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSupplier(int supplierId)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Không có quyền." });
            var s = await _db.Suppliers.FindAsync(supplierId);
            if (s == null) return Json(new { success = false, message = "Không tìm thấy." });
            if (await _db.InventoryReceipts.AnyAsync(r => r.SupplierID == supplierId)) return Json(new { success = false, message = "Không thể xóa — đã có phiếu nhập." });

            _db.Suppliers.Remove(s); await _db.SaveChangesAsync();
            return Json(new { success = true, message = "Đã xóa nhà cung cấp." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSupplier(string supplierName, string? phone, string? city, string? country)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            supplierName = (supplierName ?? "").Trim();
            if (string.IsNullOrEmpty(supplierName)) { TempData["Error"] = "Tên không trống."; return RedirectToAction("Products"); }
            if (await _db.Suppliers.AnyAsync(s => s.SupplierName == supplierName)) { TempData["Error"] = "Nhà cung cấp đã tồn tại."; return RedirectToAction("Products"); }

            _db.Suppliers.Add(new Supplier { SupplierName = supplierName, Phone = phone?.Trim(), City = city?.Trim(), Country = country?.Trim() });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã thêm nhà cung cấp!";
            return RedirectToAction("Products");
        }

        #endregion

        #region 5. QUẢN LÝ ĐƠN HÀNG, KHÁCH HÀNG & MÃ GIẢM GIÁ (Feature)

        public async Task<IActionResult> Orders(string? status, string? search)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var query = _db.Orders.Include(o => o.User).Include(o => o.OrderDetails).AsQueryable();

            if (!string.IsNullOrEmpty(status) && int.TryParse(status, out int s)) query = query.Where(o => o.Status == s);
            if (!string.IsNullOrEmpty(search)) query = query.Where(o => o.ReceiverName.Contains(search) || o.ReceiverPhone.Contains(search) || o.OrderID.ToString().Contains(search));

            ViewBag.Status = status; ViewBag.Search = search;
            return View(await query.OrderByDescending(o => o.OrderDate).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, int status)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var order = await _db.Orders.FindAsync(orderId);
            if (order != null) { order.Status = status; await _db.SaveChangesAsync(); TempData["Success"] = "Đã cập nhật trạng thái!"; }
            return RedirectToAction("Orders");
        }

        public async Task<IActionResult> Customers(string? search)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var query = _db.Users.Include(u => u.CustomerDetail).Where(u => u.Role == (int)UserRole.Customer).AsQueryable(); // 💡 ENUM
            if (!string.IsNullOrEmpty(search)) query = query.Where(u => u.FullName.Contains(search) || u.Phone.Contains(search));
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
                user.Status = user.Status == (int)UserStatus.Active ? (int)UserStatus.Inactive : (int)UserStatus.Active; // 💡 ENUM
                await _db.SaveChangesAsync();
                TempData["Success"] = user.Status == (int)UserStatus.Inactive ? "Đã khóa tài khoản!" : "Đã mở khóa tài khoản!";
            }
            return RedirectToAction("Customers");
        }

        public async Task<IActionResult> Discounts()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View(await _db.Discounts.OrderByDescending(d => d.DiscountID).ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDiscount(string code, decimal discountValue, string discountType, int quantity, DateTime expirationDate)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            code = (code ?? "").Trim().ToUpper();
            if (string.IsNullOrEmpty(code)) { TempData["Error"] = "Mã không trống."; return Redirect("/Admin/Products#discounts"); }
            if (discountValue <= 0) { TempData["Error"] = "Giá trị giảm > 0."; return Redirect("/Admin/Products#discounts"); }
            if (quantity < 1) { TempData["Error"] = "Số lượng >= 1."; return Redirect("/Admin/Products#discounts"); }
            if (await _db.Discounts.AnyAsync(d => d.Code == code)) { TempData["Error"] = "Mã đã tồn tại."; return Redirect("/Admin/Products#discounts"); }

            int typeInt = string.Equals(discountType, "percent", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            if (typeInt == 1 && discountValue > 100) { TempData["Error"] = "Giảm % không được vượt quá 100."; return Redirect("/Admin/Products#discounts"); }

            _db.Discounts.Add(new Discount { Code = code, DiscountValue = discountValue, DiscountType = typeInt, Quantity = quantity, UsedCount = 0, ExpirationDate = expirationDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59) });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã thêm mã giảm giá!";
            return Redirect("/Admin/Products#discounts");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDiscount(int discountId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var d = await _db.Discounts.FindAsync(discountId);
            if (d == null) { TempData["Error"] = "Không tìm thấy mã."; return Redirect("/Admin/Products#discounts"); }
            if (await _db.Orders.AnyAsync(o => o.DiscountID == discountId)) { TempData["Error"] = "Không thể xóa — đã dùng trong đơn hàng."; return Redirect("/Admin/Products#discounts"); }

            _db.Discounts.Remove(d); await _db.SaveChangesAsync();
            TempData["Success"] = "Đã xóa mã.";
            return Redirect("/Admin/Products#discounts");
        }

        #endregion

        #region 6. QUẢN LÝ QUẢNG CÁO & XUẤT EXCEL (Feature)

        public async Task<IActionResult> Advertisements()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var ads = await _db.Advertisements.OrderByDescending(a => a.CreatedDate).ToListAsync();
            return View(ads);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAdvertisement(string title, string position, IFormFile? imageFile)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn và cắt ảnh quảng cáo.";
                return RedirectToAction("Advertisements");
            }

            var pos = (position ?? "banner").Trim().ToLowerInvariant();
            if (pos is not ("banner" or "popup" or "sidebar"))
                pos = "banner";

            var removedSamples = await AdvertisementHelper.RemoveSampleAdvertisementsAsync(_db);
            var imageUrl = await FileHelper.UploadImageAsync(imageFile, "ads", _env);
            if (string.IsNullOrEmpty(imageUrl))
            {
                TempData["Error"] = "Không thể tải ảnh lên. Vui lòng thử lại.";
                return RedirectToAction("Advertisements");
            }

            _db.Advertisements.Add(new Advertisement
            {
                Title = (title ?? "").Trim(),
                ImageUrl = imageUrl,
                LinkUrl = null,
                Position = pos,
                IsActive = true,
                CreatedDate = DateTime.Now
            });
            await _db.SaveChangesAsync();

            var msg = "Đã thêm quảng cáo!";
            if (removedSamples > 0)
                msg += $" (Đã xóa {removedSamples} quảng cáo mẫu.)";
            TempData["Success"] = msg;
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
                return File(bytes, "text/csv; charset=utf-8", $"HoaDonNhap_{receiptId:D6}_{receipt.ImportDate:yyyyMMdd}.csv");
            }

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("PhieuNhap");
            var details = receipt.InventoryReceiptDetails.OrderBy(d => d.DetailID).ToList();
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
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"PhieuNhap_{receiptId:D6}_{receipt.ImportDate:yyyyMMdd}.xlsx");
        }

        #endregion
    }
}