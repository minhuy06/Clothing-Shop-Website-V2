using Microsoft.AspNetCore.Http;

namespace Clothing_Shop_Website.Helper
{
    /// <summary>
    /// Tiện ích điểm thưởng — đọc/ghi session.
    /// Session "Points" được cập nhật tại đăng nhập, vào giỏ hàng và sau khi đặt hàng.
    /// </summary>
    public static class RewardPointsHelper
    {
        private const string Key = "Points";

        /// <summary>Lấy điểm thưởng hiện tại từ Session (đã được sync từ DB).</summary>
        public static int GetPoints(ISession session)
            => session.GetInt32(Key) ?? 0;

        /// <summary>Ghi điểm thưởng vào Session (gọi sau khi query DB).</summary>
        public static void SyncSession(ISession session, int points)
            => session.SetInt32(Key, points);
    }
}
