using Locus.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Locus
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddSingleton<IConnectionFactory, ConnectionFactory>();
            services.AddSingleton<IRepository, Repository>();
            services.AddSingleton<ILogger, Logger>();
            services.AddSingleton<ILogger, Logger>();
            services.AddSingleton(typeof(IActionTransferObject<>), typeof(ActionTransferObject<>));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            } 
            else
            {
                app.UseStatusCodePagesWithReExecute("/Error/Warning/{0}");
                app.UseExceptionHandler("/Error/Exception");
            }
            app.UseStaticFiles();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
