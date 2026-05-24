using System.Threading;
using System.Threading.Tasks;
using Clothing_Shop_Website.ViewModels;
using System.Collections.Generic;

namespace Clothing_Shop_Website.Services
{
    public interface ICubeMdxAnalyticsService
    {
        Task<AdminDashboardViewModel> BuildDashboardAsync(string? seasonFilter, string? ageGroupFilter, CancellationToken cancellationToken = default);
        Task<List<TopSellerCubeRow>> GetTopProductsForCategoryAsync(string categoryName, int limit = 5, CancellationToken ct = default);
        Task<List<TopSellerCubeRow>> GetForecastNext3MonthsAsync(CancellationToken ct = default);
    }
}
