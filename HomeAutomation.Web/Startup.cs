using HomeAutomation.Web.Data;
using HomeAutomation.Web.Helpers;
using HomeAutomation.Web.Helpers.Interfaces;
using HomeAutomation.Web.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
            services.Configure<ComputerOptions>(_config.GetSection("Computer"));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

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
