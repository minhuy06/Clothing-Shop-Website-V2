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

namespace Clothing_Shop_Website.Controllers
{
    // Helper for SetShifts action
    public class ShiftInput
    {
        public int ShiftType { get; set; }
        public string DayOfWeek { get; set; } = "";
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
        public async Task<IActionResult> Products(string? search, int? categoryId, string? season, int page = 1)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            const int pageSize = 10;
            if (page < 1) page = 1;

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
                    stock = p.ProductSizes.Sum(s => s.StockQuantity),
                    sizes = p.ProductSizes
                        .OrderBy(s => s.SizeName)
                        .Select(s => new { s.SizeName, s.StockQuantity })
                        .ToList()
                }
            });
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

            var p = await _db.Products.FindAsync(productID);
            if (p == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm.";
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
            p.ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? p.ImageUrl : imageUrl.Trim();
            p.Description = description?.Trim();
            p.Color = string.IsNullOrWhiteSpace(color) ? null : color.Trim();
            p.Style = string.IsNullOrWhiteSpace(style) ? null : style.Trim();
            p.Material = string.IsNullOrWhiteSpace(material) ? null : material.Trim();

            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật sản phẩm #" + productID.ToString("D3") + "!";
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
        public async Task<IActionResult> ExportReceipt(int receiptId)
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
                return RedirectToAction("Dashboard");
            }

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

    }

    // ViewModel cho SetShifts
    public class SetShiftsRequest
    {
        public int UserId { get; set; }
        public List<ShiftInput>? Shifts { get; set; }
    }
}