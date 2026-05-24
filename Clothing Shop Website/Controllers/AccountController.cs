using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Clothing_Shop_Website.Data;
using Clothing_Shop_Website.Models;
using Clothing_Shop_Website.Helper;
using Clothing_Shop_Website.ViewModels;

namespace Clothing_Shop_Website.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        public AccountController(AppDbContext db) { _db = db; }

        

        private void SetUserSession(User user)
        {
            HttpContext.Session.SetInt32("UserId", user.UserID);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetString("Phone", user.Phone);
            HttpContext.Session.SetInt32("Role", user.Role);
            HttpContext.Session.SetInt32("Points", user.RewardPoints);
        }

        // ── LOGIN ──
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
                return RedirectToAction("Profile");
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
            var hashed = (password);
            var user = await _db.Users
                .Include(u => u.CustomerDetail)
                .FirstOrDefaultAsync(u => u.Phone == phone && u.Password == hashed);
            if (user == null) { TempData["Error"] = "Số điện thoại hoặc mật khẩu không đúng!"; return View(); }
            if (user.Status == 0) { TempData["Error"] = "Tài khoản của bạn đã bị khóa!"; return View(); }
            SetUserSession(user);
            return (user.Role == 0 || user.Role == 1)
                ? RedirectToAction("Dashboard", "Admin")
                : RedirectToAction("Profile");
        }

        // ── REGISTER ──
        [HttpPost]
        public async Task<IActionResult> Register(string fullName, string phone, string password, string confirmPassword)
        {
            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(phone) ||
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
            { TempData["RegError"] = "Vui lòng điền đầy đủ thông tin!"; return View("Login"); }

            if (password != confirmPassword)
            { TempData["RegError"] = "Mật khẩu xác nhận không khớp!"; return View("Login"); }

            if (password.Length < 6)
            { TempData["RegError"] = "Mật khẩu phải ít nhất 6 ký tự!"; return View("Login"); }

            if (await _db.Users.AnyAsync(u => u.Phone == phone))
            { TempData["RegError"] = "Số điện thoại này đã được đăng ký!"; return View("Login"); }

            var newUser = new User
            {
                FullName = fullName,
                Phone = phone,
                Password = SecurityHelper.HashPassword(password),
                Role = 2,
                Status = 1,
                RewardPoints = 0,
                Gender = 0,
                DateOfBirth = null
            };
            _db.Users.Add(newUser);
            await _db.SaveChangesAsync();
            SetUserSession(newUser);
            TempData["Success"] = "Tạo tài khoản thành công!";
            return RedirectToAction("Profile");
        }

        // ── PROFILE ──
        [HttpGet]
        public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var user = _db.Users.Include(u => u.CustomerDetail).Include(u => u.UserAddresses).FirstOrDefault(u => u.UserID == userId);
            if (user == null)
                return NotFound();

            var model = new ProfileViewModel
            {
                FullName = user.FullName,
                Phone = user.Phone,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                RewardPoints = user.RewardPoints,
                Status = user.Status,
                UserAddresses = user.UserAddresses.ToList()
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult EditProfile() {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login");

                var user = _db.Users.FirstOrDefault(u => u.UserID == userId);
                if (user == null)
                    return NotFound();

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
        public IActionResult EditProfile(EditProfileViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(model);

            var user = _db.Users.FirstOrDefault(u => u.UserID == userId);
            if (user == null) return NotFound();

            user.FullName = model.FullName;
            user.Phone = model.Phone;
            user.DateOfBirth = model.DateOfBirth;
            user.Gender = model.Gender;

            _db.Update(user);
            _db.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
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

        // ── LOGOUT ──
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ── ADDRESS ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress([Bind("FullName,Phone,Province_City,DetailedAddress")] UserAddress address)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login");

            if (!ModelState.IsValid)
            {
                // Lưu thông báo lỗi đầu tiên để hiển thị
                var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                TempData["Error"] = firstError ?? "Dữ liệu địa chỉ không hợp lệ!";
                return RedirectToAction("Profile");
            }

            address.UserID = userId.Value;

            try
            {
                _db.UserAddresses.Add(address);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã thêm địa chỉ mới thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra trong quá trình lưu địa chỉ: " + ex.Message;
            }

            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(int addressId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");
            var address = await _db.UserAddresses
                .FirstOrDefaultAsync(a => a.AddressID == addressId && a.UserID == userId);
            if (address != null) { _db.UserAddresses.Remove(address); await _db.SaveChangesAsync(); }
            return RedirectToAction("Profile");
        }
    }
}