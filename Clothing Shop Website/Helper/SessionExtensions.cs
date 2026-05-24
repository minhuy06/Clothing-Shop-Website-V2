using Clothing_Shop_Website.Models;
using Microsoft.AspNetCore.Http;

namespace Clothing_Shop_Website.Helper
{
    public static class SessionExtensions
    {
        public static void SetUserSession(this ISession session, User user)
        {
            session.SetInt32("UserId", user.UserID);
            session.SetString("FullName", user.FullName);
            session.SetString("Phone", user.Phone);
            session.SetInt32("Role", user.Role);
            session.SetInt32("Points", user.RewardPoints);
        }

        public static int? GetUserId(this ISession session)
        {
            return session.GetInt32("UserId");
        }
    }
}
