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
using Clothing_Shop_Website.Helper;
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
            foreach (var prod in topProducts)
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

        // Quản lý nhân viên
        public async Task<IActionResult> StaffMembers(string? search)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var querry = _db.Users
                .Include(u => u.StaffDetail)
                    .ThenInclude(sd => sd.StaffShifts)
                .Where(u => u.Role == 1)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                querry = querry.Where(u => u.FullName.Contains(search) || u.Phone.Contains(search));

            return View(await querry.OrderByDescending(u => u.UserID).ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStaff(string fullName, string phone, string password, int gender, DateTime dateOfBirth, decimal salary, DateTime hireDate, List<string> selectedShifts)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

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
                    Role = 1,
                    Status = 1,
                    Gender = gender,
                    DateOfBirth = dateOfBirth
                };

                _db.Users.Add(newStaff);
                await _db.SaveChangesAsync();

                // Lưu vào StaffDetail
                _db.StaffDetails.Add(new StaffDetail { UserID = newStaff.UserID, Salary = salary, HireDate = hireDate });
                await _db.SaveChangesAsync();

                // Lịch làm việc
                if (selectedShifts != null && selectedShifts.Any())
                {
                    foreach(var shiftCode in selectedShifts)
                    {
                        // Cắt chuỗi dựa vào '_'
                        var parts = shiftCode.Split('_');
                        if (parts.Length == 2)
                        {
                            string dayOfWeek = parts[0];
                            if(int.TryParse(parts[1], out int shiftType))
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
                .FirstOrDefaultAsync(u => u.UserID == userId && u.Role == 1);

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

                if (user.StaffDetail != null)
                {
                    user.StaffDetail.Salary = salary;
                }

                // Xóa lịch cũ
                if (user.StaffDetail != null && user.StaffDetail.StaffShifts.Any())
                {
                    _db.StaffShifts.RemoveRange(user.StaffDetail.StaffShifts);
                }

                // Thêm lịch mới
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

        // Xóa nhân viên
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStaff(int userId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var user = await _db.Users
            .Include(u => u.StaffDetail)
                .ThenInclude(sd => sd.StaffShifts)
            .FirstOrDefaultAsync(u => u.UserID == userId && u.Role == 1);

            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy nhân viên!";
                return RedirectToAction("StaffMembers");
            }

            user.Status = 0;

            if (user.StaffDetail != null && user.StaffDetail.StaffShifts.Any())
                _db.StaffShifts.RemoveRange(user.StaffDetail.StaffShifts);

            await _db.SaveChangesAsync();

            TempData["Success"] = $"Đã chuyển nhân viên {user.FullName} sang trạng thái Nghỉ việc (Bảo lưu dữ liệu lịch sử thành công).";
            return RedirectToAction("StaffMembers");
        }
    }
}