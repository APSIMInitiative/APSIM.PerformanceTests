using System;
using APSIM.POStats.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite.Infrastructure.Internal;
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
            {
                Console.WriteLine("Using SQLite database");

                // If the connection string is just a file path (e.g. "portal.db"),
                // convert it to a proper Data Source connection string and ensure
                // the directory and file exist so the provider can open it.
                string sqliteConnectionString = connectionString;
                if (!connectionString.Contains("="))
                {
                    var dbPath = System.IO.Path.IsPathRooted(connectionString)
                                 ? connectionString
                                 : System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), connectionString);

                    var dbDir = System.IO.Path.GetDirectoryName(dbPath);
                    if (!string.IsNullOrEmpty(dbDir) && !System.IO.Directory.Exists(dbDir))
                        System.IO.Directory.CreateDirectory(dbDir);

                    if (!System.IO.File.Exists(dbPath))
                        System.IO.File.Create(dbPath).Dispose();

                    sqliteConnectionString = $"Data Source={dbPath}";
                }

                services.AddDbContext<StatsDbContext>(options => options.UseLazyLoadingProxies().UseSqlite(sqliteConnectionString));
            }
            else
            {
                Console.WriteLine("Using MySQL database");
                var serverVersion = new MySqlServerVersion(new Version(10, 0, 0));
                services.AddDbContext<StatsDbContext>(options => options.UseLazyLoadingProxies().UseMySql(
                    connectionString,
                    serverVersion,
                    mySqlOptionsAction: sqlOptions =>
                    {
                        sqlOptions.CommandTimeout(60);
                        sqlOptions.EnableRetryOnFailure(                           
                            maxRetryCount: 20,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null
                        );
                    })
                );
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