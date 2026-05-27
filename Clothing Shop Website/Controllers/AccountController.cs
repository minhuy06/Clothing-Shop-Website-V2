using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Clothing_Shop_Website.Data;
using Clothing_Shop_Website.Models;
using Clothing_Shop_Website.Enums;
using Clothing_Shop_Website.ViewModels;
using Clothing_Shop_Website.Helper;

namespace Clothing_Shop_Website.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;

        public AccountController(AppDbContext db)
        {
            _db = db;
        }

        // ĐĂNG NHẬP
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetUserId() != null)
            {
                var role = HttpContext.Session.GetInt32("Role");
                if (role == (int)UserRole.Admin) return RedirectToAction("Dashboard", "Admin");
                if (role == (int)UserRole.Staff) return RedirectToAction("Inventory", "Staff");
                return RedirectToAction("Profile");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string phone, string password)
        {
            if (string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(password))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin!";
                return View();
            }

            var hashed = SecurityHelper.HashPassword(password);

            var user = await _db.Users
                .Include(u => u.CustomerDetail)
                .FirstOrDefaultAsync(u => u.Phone == phone && u.Password == hashed);

            if (user == null) { TempData["Error"] = "Số điện thoại hoặc mật khẩu không đúng!"; return View(); }

            if (user.Status == (int)UserStatus.Inactive) { TempData["Error"] = "Tài khoản của bạn đã bị khóa!"; return View(); }

            HttpContext.Session.SetUserSession(user);

            if (user.Role == (int)UserRole.Admin) return RedirectToAction("Dashboard", "Admin");
            if (user.Role == (int)UserRole.Staff) return RedirectToAction("Inventory", "Staff");

            return RedirectToAction("Profile");
        }

        // ĐĂNG KÝ
        [HttpPost]
        public async Task<IActionResult> Register(string fullName, string phone, string password, string confirmPassword)
        {
            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
            { TempData["RegError"] = "Vui lòng điền đầy đủ thông tin!"; return View("Login"); }

            if (password != confirmPassword)
            { TempData["RegError"] = "Mật khẩu xác nhận không khớp!"; return View("Login"); }

            if (password.Length < 6)
            { TempData["RegError"] = "Mật khẩu phải ít nhất 6 ký tự!"; return View("Login"); }

            if (await _db.Users.AnyAsync(u => u.Phone == phone))
            { TempData["RegError"] = "Số điện thoại này đã được đăng ký!"; return View("Login"); }

            var newUser = new User
            {
                FullName = fullName.Trim(),
                Phone = phone.Trim(),
                Password = SecurityHelper.HashPassword(password),
                Role = (int)UserRole.Customer,
                Status = (int)UserStatus.Active,
                Gender = 0,
                DateOfBirth = null,
                CustomerDetail = new CustomerDetail
                {
                    RewardPoints = 0,
                    MembershipTier = "Đồng"
                }
            };

            _db.Users.Add(newUser);
            await _db.SaveChangesAsync();
            HttpContext.Session.SetUserSession(newUser);
            RewardPointsHelper.SyncSession(HttpContext.Session, 0);

            TempData["Success"] = "Tạo tài khoản thành công!";
            return RedirectToAction("Profile");
        }

        // QUẢN LÝ HỒ SƠ
        [HttpGet]
        public async Task<IActionResult> Profile(string? tab)
        {
            var userId = HttpContext.Session.GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var user = await _db.Users
                .Include(u => u.CustomerDetail)
                .Include(u => u.UserAddresses)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null) return NotFound();

            var year = MembershipTierHelper.CurrentYear;
            var yearStart = new DateTime(year, 1, 1);
            var nextYearStart = yearStart.AddYears(1);
            var yearlySpend = await _db.Orders.AsNoTracking()
                .Where(o => o.UserID == userId
                            && o.Status != (int)OrderStatus.Cancelled
                            && o.OrderDate >= yearStart && o.OrderDate < nextYearStart)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            // Hạng hiển thị lấy từ database (CustomerDetails.MembershipTier)
            var membershipTier = MembershipTierHelper.NormalizeTier(user.CustomerDetail?.MembershipTier);

            var model = new ProfileViewModel
            {
                FullName = user.FullName,
                Phone = user.Phone,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                RewardPoints = user.RewardPoints,
                MembershipTier = membershipTier,
                YearlySpend = yearlySpend,
                Status = user.Status,
                UserAddresses = user.UserAddresses
                    .OrderByDescending(a => a.IsDefault)
                    .ThenByDescending(a => a.AddressID)
                    .ToList()
            };

            ViewData["ProfileTab"] = !string.IsNullOrWhiteSpace(tab)
                ? tab.Trim().ToLowerInvariant()
                : TempData["Tab"]?.ToString() ?? "";

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var userId = HttpContext.Session.GetUserId();
            if (userId == null) return RedirectToAction("Login");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null) return NotFound();

            var model = new EditProfileViewModel
            {
                FullName = user.FullName,
                Phone = user.Phone,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            var userId = HttpContext.Session.GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid) return View(model);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null) return NotFound();

            user.FullName = model.FullName.Trim();
            user.Phone = model.Phone.Trim();
            user.DateOfBirth = model.DateOfBirth;
            user.Gender = model.Gender;

            _db.Update(user);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var userId = HttpContext.Session.GetUserId();
            if (userId == null) return RedirectToAction("Login");

            var user = await _db.Users.FindAsync(userId);
            if (user == null) return RedirectToAction("Login");

            if (user.Password != SecurityHelper.HashPassword(currentPassword))
            { TempData["PassError"] = "Mật khẩu hiện tại không đúng!"; return RedirectToAction("Profile"); }

            if (newPassword != confirmPassword)
            { TempData["PassError"] = "Mật khẩu xác nhận không khớp!"; return RedirectToAction("Profile"); }

            if (newPassword.Length < 6)
            { TempData["PassError"] = "Mật khẩu mới phải ít nhất 6 ký tự!"; return RedirectToAction("Profile"); }

            user.Password = SecurityHelper.HashPassword(newPassword);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Profile");
        }

        // QUẢN LÝ SỔ ĐỊA CHỈ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress([Bind("FullName,Phone,Province_City,DetailedAddress")] UserAddress address)
        {
            var userId = HttpContext.Session.GetUserId();
            if (userId == null) return RedirectToAction("Login");

            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                TempData["Error"] = firstError ?? "Dữ liệu địa chỉ không hợp lệ!";
                TempData["Tab"] = "address";
                return RedirectToAction("Profile");
            }

            var isFirst = !await _db.UserAddresses.AnyAsync(a => a.UserID == userId);

            address.UserID = userId.Value;
            address.IsDefault = isFirst;

            try
            {
                _db.UserAddresses.Add(address);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã thêm địa chỉ mới thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
            }

            TempData["Tab"] = "address";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAddress(int addressId, string fullName, string phone, string province_City, string detailedAddress)
        {
            var userId = HttpContext.Session.GetUserId();
            if (userId == null) return RedirectToAction("Login");

            var address = await _db.UserAddresses
                .FirstOrDefaultAsync(a => a.AddressID == addressId && a.UserID == userId);

            if (address == null) return RedirectToAction("Profile");

            address.FullName = fullName;
            address.Phone = phone;
            address.Province_City = province_City;
            address.DetailedAddress = detailedAddress;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật địa chỉ!";
            TempData["Tab"] = "address";

            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<IActionResult> SetDefaultAddress(int addressId)
        {
            var userId = HttpContext.Session.GetUserId();
            if (userId == null) return RedirectToAction("Login");

            var addresses = await _db.UserAddresses.Where(a => a.UserID == userId).ToListAsync();
            var target = addresses.FirstOrDefault(a => a.AddressID == addressId);

            if (target == null) return RedirectToAction("Profile");

            foreach (var a in addresses)
            {
                a.IsDefault = (a.AddressID == addressId);
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã đặt địa chỉ mặc định!";
            TempData["Tab"] = "address";

            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(int addressId)
        {
            var userId = HttpContext.Session.GetUserId();
            if (userId == null) return RedirectToAction("Login");

            var address = await _db.UserAddresses
                .FirstOrDefaultAsync(a => a.AddressID == addressId && a.UserID == userId);

            if (address != null)
            {
                var wasDefault = address.IsDefault;
                _db.UserAddresses.Remove(address);
                await _db.SaveChangesAsync();

                if (wasDefault)
                {
                    var next = await _db.UserAddresses
                        .Where(a => a.UserID == userId)
                        .OrderByDescending(a => a.AddressID)
                        .FirstOrDefaultAsync();

                    if (next != null)
                    {
                        next.IsDefault = true;
                        await _db.SaveChangesAsync();
                    }
                }
            }
            TempData["Tab"] = "address";
            return RedirectToAction("Profile");
        }

        // ĐĂNG XUẤT
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}