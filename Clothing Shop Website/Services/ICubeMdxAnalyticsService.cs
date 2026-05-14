using System.Threading;
using System.Threading.Tasks;
using Clothing_Shop_Website.Models.ViewModels;

namespace Clothing_Shop_Website.Services
{
    public interface ICubeMdxAnalyticsService
    {
        Task<AdminDashboardViewModel> BuildDashboardAsync(string? seasonFilter, string? ageGroupFilter, CancellationToken cancellationToken = default);
    }
}
