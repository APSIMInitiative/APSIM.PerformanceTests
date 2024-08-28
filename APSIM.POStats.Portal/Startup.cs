using System;
using APSIM.POStats.Portal.Data;
using APSIM.POStats.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace APSIM.POStats.Portal
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
            });

            services.AddControllers();
            services.AddRazorPages();

            string connectionString = Environment.GetEnvironmentVariable("PORTAL_DB");
            if (string.IsNullOrEmpty(connectionString))
                throw new Exception("Cannot find environment variable PORTAL_DB");

            if (connectionString.Contains(".db"))
                services.AddDbContext<StatsDbContext>(options => options.UseLazyLoadingProxies().UseSqlite(connectionString));
            else
            {
                var serverVersion = new MySqlServerVersion(new Version(10, 0, 0));
                services.AddDbContext<StatsDbContext>(options => options.UseLazyLoadingProxies().UseMySql(connectionString, serverVersion));
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<StatsDbContext>();
                context.Database.EnsureCreated();
            }

            app.UseExceptionHandler("/Error");
            app.UseHsts();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
            });
        }
    }
}