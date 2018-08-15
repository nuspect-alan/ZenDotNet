/*************************************************************************
 * Copyright (c) 2015, 2018 Zenodys BV
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *
 * Contributors:
 *    Tomaž Vinko
 *   
 **************************************************************************/
#if NETCOREAPP2_0
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Reflection;
using ZenWebServer;

public class Startup
{
    #region Fields
    ZenoConfigContainer _zenoConf;
    public IConfiguration Configuration { get; }
    public object UrlParameter { get; private set; }
    #endregion

    public Startup(IConfiguration configuration, ZenoConfigContainer zenoConf)
    {
        Configuration = configuration;
        this._zenoConf = zenoConf;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        Assembly Controllers = Assembly.LoadFile(Path.Combine(System.Environment.CurrentDirectory, "tmp", "WebServerController_" + _zenoConf.element.ID + ".zen"));
        services.AddMvc()
            .AddApplicationPart(Controllers)
            .AddControllersAsServices()
            .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
    }

    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
        //if (env.IsDevelopment())
        //{
        app.UseDeveloperExceptionPage();
        /* }
         else
         {
             app.UseExceptionHandler("/Home/Error");
         }*/

        app.UseStaticFiles();



        app.UseMvc(routes =>
        {

            routes.MapRoute(
                name: "default",
                template: "{controller}/{action}"
                );

            routes.MapRoute(
                    name: "nodeExecute",
                    template: "{controller}/{action}/{nodeId}"
                    );
             
             routes.MapRoute(
                    name: "getNodeValue",
                    template: "{controller}/{action}/{nodeId}/{jsonPath}/{isTable}/{nodeDependency}"
                    );

        });
    }
}
#endif