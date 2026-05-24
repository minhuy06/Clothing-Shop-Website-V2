using System;
using System.Security.Cryptography;
using System.Text;

namespace Clothing_Shop_Website.Helper
{
    public static class SecurityHelper
    {
        public static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}
