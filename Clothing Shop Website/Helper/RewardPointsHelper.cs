using System.Linq;
using System.Threading.Tasks;
using Clothing_Shop_Website.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Clothing_Shop_Website.Helper
{
    public static class RewardPointsHelper
    {
        /// <summary>Lấy điểm thưởng từ bảng CustomerDetails (database).</summary>
        public static async Task<int> GetPointsAsync(AppDbContext db, int userId)
        {
            return await db.CustomerDetails.AsNoTracking()
                .Where(c => c.UserID == userId)
                .Select(c => c.RewardPoints)
                .FirstOrDefaultAsync();
        }

        public static void SyncSession(ISession session, int points)
        {
            session.SetInt32("Points", points);
        }
    }
}
