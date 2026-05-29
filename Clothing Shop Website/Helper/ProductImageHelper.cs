using System;

namespace Clothing_Shop_Website.Helper
{
    /// <summary>
    /// Chuẩn hóa đường dẫn ảnh sản phẩm từ DB (vd. /images/prod/1.jpg → file trong DataWarehouse_Scripts/images/prod).
    /// </summary>
    public static class ProductImageHelper
    {
        public const string PlaceholderPath = "/images/placeholder-product.svg";

        public static string Resolve(string? imageUrl, int productId)
        {
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                var url = imageUrl.Trim();

                if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    return ProdPath(productId);
                }

                if (!url.StartsWith("/"))
                    url = "/" + url;

                return url;
            }

            return ProdPath(productId);
        }

        public static string ProdPath(int productId) => $"/images/prod/{productId}.jpg";
    }
}
