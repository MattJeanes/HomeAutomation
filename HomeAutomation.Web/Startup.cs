using HomeAutomation.Web.Data;
using HomeAutomation.Web.Helpers;
using HomeAutomation.Web.Helpers.Interfaces;
using HomeAutomation.Web.Middleware;
using HomeAutomation.Web.Services;
using HomeAutomation.Web.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace HomeAutomation.Web
{
    public class Startup
    {
        public readonly IConfiguration _config;

        public Startup(IConfiguration configuration)
        {
            _config = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddTransient<IWakeOnLANHelper, WakeOnLANHelper>();
            services.AddHttpClient<ITeslaService, TeslaService>(x => x.BaseAddress = new Uri(_config.GetValue<string>("Tesla:BaseUrl")));
            services.Configure<ComputerOptions>(_config.GetSection("Computer"));
            services.Configure<TeslaOptions>(_config.GetSection("Tesla"));
            services.AddMemoryCache();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseHttpsRedirection(); // handled by docker
            }

            app.UseDefaultFiles();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseApiKeyMiddleware(_config["Key"]);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
