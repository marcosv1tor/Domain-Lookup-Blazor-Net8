using System;
using Desafio.Umbler.Application.Contracts;
using Desafio.Umbler.Application.Services;
using Desafio.Umbler.Infrastructure.Clock;
using Desafio.Umbler.Infrastructure.External;
using Desafio.Umbler.Infrastructure.Persistence;
using Desafio.Umbler.Models;
using Desafio.Umbler.Web.Clients;
using Desafio.Umbler.Web.Middleware;
using DnsClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Desafio.Umbler
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }

        public IWebHostEnvironment Environment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 27));

            services.AddDbContext<DatabaseContext>(dbContextOptions =>
            {
                dbContextOptions.UseMySql(connectionString, serverVersion);

                if (Environment.IsDevelopment())
                {
                    dbContextOptions
                        .LogTo(Console.WriteLine, LogLevel.Information)
                        .EnableSensitiveDataLogging()
                        .EnableDetailedErrors();
                }
            });

            services.AddSingleton<ILookupClient>(_ => new LookupClient());

            services.AddSingleton<IClock, SystemClock>();
            services.AddScoped<IDomainRepository, DomainRepository>();
            services.AddScoped<IDnsLookupGateway, DnsLookupGateway>();
            services.AddScoped<IWhoisGateway, WhoisGateway>();
            services.AddScoped<IDomainLookupService, DomainLookupService>();

            services.AddHttpClient<IDomainApiClient, DomainApiClient>();

            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddControllersWithViews();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (!env.IsDevelopment())
            {
                app.UseHsts();
            }

            app.UseMiddleware<GlobalExceptionMiddleware>();

            app.UseStaticFiles();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
