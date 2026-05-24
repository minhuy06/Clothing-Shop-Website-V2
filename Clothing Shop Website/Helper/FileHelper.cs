using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Clothing_Shop_Website.Helper
{
    public static class FileHelper
    {
        public static async Task<string?> UploadImageAsync(IFormFile? file, string folderName, IWebHostEnvironment env)
        {
            if (file == null || file.Length == 0) return null;

            var ext = Path.GetExtension(file.FileName);
            if (ext.Length > 10) ext = "";
            var safeName = $"{Guid.NewGuid():N}{ext}";

            var dir = Path.Combine(env.WebRootPath, "uploads", folderName);
            Directory.CreateDirectory(dir);

            var fullPath = Path.Combine(dir, safeName);
            await using (var fs = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(fs);
            }
            return $"/uploads/{folderName}/{safeName}";
        }
    }
}