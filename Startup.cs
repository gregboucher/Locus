using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Locus.Models;
using Locus.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
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
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseStaticFiles();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                
                //app.UseEndpoints(endpoints =>
                //{
                //    endpoints.MapControllerRoute(
                //        name: "default",
                //        pattern: "{controller=Dashboard}/{action=Index}/{id?}");
                //});
            });
        }
    }
}
