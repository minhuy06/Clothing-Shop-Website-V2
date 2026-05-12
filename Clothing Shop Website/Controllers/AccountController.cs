using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Clothing_Shop_Website.Data;
using Clothing_Shop_Website.Models;
using System.Security.Cryptography;
using System.Text;

namespace Clothing_Shop_Website.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        public AccountController(AppDbContext db) { _db = db; }

        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLower();
        }

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
            var hashed = HashPassword(password);
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Phone == phone && u.Password == hashed);
            if (user == null) { TempData["Error"] = "Số điện thoại hoặc mật khẩu không đúng!"; return View(); }
            if (user.Status == 0) { TempData["Error"] = "Tài khoản của bạn đã bị khóa!"; return View(); }
            SetUserSession(user);
            return user.Role == 1
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
                Password = HashPassword(password),
                Role = 0,
                Status = 1,
                RewardPoints = 0
            };
            _db.Users.Add(newUser);
            await _db.SaveChangesAsync();
            SetUserSession(newUser);
            TempData["Success"] = "Tạo tài khoản thành công!";
            return RedirectToAction("Profile");
        }

        // ── PROFILE ──
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");
            var user = await _db.Users
                .Include(u => u.UserAddresses)
                .FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null) return RedirectToAction("Login");
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string fullName, string phone)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return RedirectToAction("Login");
            user.FullName = fullName;
            user.Phone = phone;
            await _db.SaveChangesAsync();
            HttpContext.Session.SetString("FullName", fullName);
            HttpContext.Session.SetString("Phone", phone);
            TempData["Success"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return RedirectToAction("Login");
            if (user.Password != HashPassword(currentPassword))
            { TempData["PassError"] = "Mật khẩu hiện tại không đúng!"; return RedirectToAction("Profile"); }
            if (newPassword != confirmPassword)
            { TempData["PassError"] = "Mật khẩu xác nhận không khớp!"; return RedirectToAction("Profile"); }
            if (newPassword.Length < 6)
            { TempData["PassError"] = "Mật khẩu mới phải ít nhất 6 ký tự!"; return RedirectToAction("Profile"); }
            user.Password = HashPassword(newPassword);
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
        public async Task<IActionResult> AddAddress(string fullName, string phone, string province_City, string detailedAddress)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");
            var address = new UserAddress
            {
                UserID = userId.Value,
                FullName = fullName,
                Phone = phone,
                Province_City = province_City,
                DetailedAddress = detailedAddress
            };
            _db.UserAddresses.Add(address);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã thêm địa chỉ!";
            return RedirectToAction("Profile");
        }

        [HttpPost]
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