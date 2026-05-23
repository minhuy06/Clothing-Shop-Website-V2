using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Clothing_Shop_Website.Data
{
    /// <summary>
    /// Chạy script Seed_Realistic_Data.sql khi database chưa có sản phẩm.
    /// </summary>
    public static class DbSeedRunner
    {
        public static async Task EnsureSeedAsync(
            AppDbContext db,
            IConfiguration configuration,
            IHostEnvironment env,
            ILogger logger)
        {
            if (await db.Products.AsNoTracking().AnyAsync())
                return;

            var path = Path.GetFullPath(Path.Combine(
                env.ContentRootPath,
                "..",
                "DataWarehouse_Scripts",
                "Seed_Realistic_Data.sql"));

            if (!File.Exists(path))
            {
                logger.LogWarning("Không tìm thấy file seed: {Path}", path);
                return;
            }

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                logger.LogWarning("Chưa cấu hình DefaultConnection — bỏ qua seed.");
                return;
            }

            logger.LogInformation("Database trống — đang nạp dữ liệu từ Seed_Realistic_Data.sql ...");

            var sql = await File.ReadAllTextAsync(path);
            var batches = Regex.Split(sql, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            foreach (var batch in batches)
            {
                var trimmed = batch.Trim();
                if (trimmed.Length == 0)
                    continue;

                if (trimmed.StartsWith("USE ", StringComparison.OrdinalIgnoreCase))
                    continue;

                await using var cmd = new SqlCommand(trimmed, conn) { CommandTimeout = 180 };
                await cmd.ExecuteNonQueryAsync();
            }

            var count = await db.Products.AsNoTracking().CountAsync();
            logger.LogInformation("Đã nạp seed xong — {Count} sản phẩm trong database.", count);
        }
    }
}
