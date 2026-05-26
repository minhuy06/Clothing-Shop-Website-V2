using Clothing_Shop_Website.Data;
using Clothing_Shop_Website.Options;
using Clothing_Shop_Website.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Clothing_Shop_Website
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Kết nối Database
            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));

            // Cấu hình SSAS Cube Analytics
            services.Configure<CubeMdxOptions>(Configuration.GetSection(CubeMdxOptions.SectionName));
            services.AddScoped<ICubeMdxAnalyticsService, CubeMdxAnalyticsService>();

            // Cấu hình Form Upload (Giới hạn 25MB)
            services.Configure<FormOptions>(o =>
            {
                o.MultipartBodyLengthLimit = 25 * 1024 * 1024;
            });

            // Cấu hình Session
            services.AddSession(options =>
            {
                options.IdleTimeout = System.TimeSpan.FromDays(7);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            services.AddControllersWithViews()
                    .AddRazorRuntimeCompilation();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // Code First: C# model → migration → DB (Update-Database hoặc AutoMigrate)
                if (Configuration.GetValue<bool>("Database:AutoMigrate"))
                    db.Database.Migrate();
            }

            if (env.IsDevelopment())
            {
                // Hiển thị lỗi chi tiết khi đang code
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // Khởi động Session trước Authorization
            app.UseSession();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}