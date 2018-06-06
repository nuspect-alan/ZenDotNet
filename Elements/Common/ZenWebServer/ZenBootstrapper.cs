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
using Microsoft.Extensions.DependencyModel;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
#endif
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Bootstrapper;
using Nancy.Configuration;
using Nancy.Conventions;
using Nancy.TinyIoc;
using System.IO;
using System.Globalization;
using ZenCommon;

public class ZenBootstrapper : DefaultNancyBootstrapper
{
    #region Constructors
    public ZenBootstrapper(IElement element)
    {
        this._element = element;
    }
    #endregion

    #region Fields
    #region _element
    IElement _element;
    #endregion
    #endregion

    #region Overrides
    #region ConfigureApplicationContainer
    protected override void ConfigureApplicationContainer(TinyIoCContainer container)
    {
        //base.ConfigureApplicationContainer(container);
        // We don't call "base" here to prevent auto-discovery of
        // types/dependencies
    }
    #endregion

    #region ApplicationStartup
    protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
    {
        base.ApplicationStartup(container, pipelines);

        container.Register<IElement>(_element);
        this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
        {
            string viewsRoot = _element.GetElementProperty("VIEWS_ROOT").Trim().EndsWith(Path.AltDirectorySeparatorChar.ToString()) ? _element.GetElementProperty("VIEWS_ROOT").Trim() : string.Concat(_element.GetElementProperty("VIEWS_ROOT").Trim(), Path.AltDirectorySeparatorChar);
            return string.Concat(viewsRoot, viewName);
        });
    }
    #endregion

    
    public override void Configure(INancyEnvironment environment)
    {
        environment.Tracing(
            enabled: true,
            displayErrorTraces: true);
            
    }

    #region ConfigureRequestContainer
    protected override void ConfigureRequestContainer(TinyIoCContainer container, NancyContext context)
    {
        base.ConfigureRequestContainer(container, context);

        // Here we register our user mapper as a per-request singleton.
        // As this is now per-request we could inject a request scoped
        // database "context" or other request scoped services.
        container.Register<IUserMapper, UserMapper>();
    }
    #endregion

#if NETCOREAPP2_0
   public  CultureInfo aa( NancyContext ctx, GlobalizationConfiguration g)
    {
        Console.WriteLine(CultureInfo.CurrentCulture.DisplayName);
        return CultureInfo.CurrentCulture;
    }
#endif
    protected override void ConfigureConventions(NancyConventions nancyConventions)
     {
#if NETCOREAPP2_0
        List<Func<NancyContext, GlobalizationConfiguration, CultureInfo>> aa1 = new List<Func<NancyContext, GlobalizationConfiguration, CultureInfo>>
        {
            aa
        };
        DefaultCultureConventions a = new DefaultCultureConventions();
        a.Initialise(nancyConventions);
        nancyConventions.CultureConventions.Add(aa);
        
#endif
        nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("ZenSystemContent", @"ZenSystemContent"));
         base.ConfigureConventions(nancyConventions);
     }

    #region RequestStartup
    protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context)
    {
        //context.Culture = CultureInfo.CurrentCulture;
        // At request startup we modify the request pipelines to
        // include forms authentication - passing in our now request
        // scoped user name mapper.
        //
        // The pipelines passed in here are specific to this request,
        // so we can add/remove/update items in them as we please.

        if (_element.GetElementProperty("AUTHENTICATION") == "1")
        {
            FormsAuthentication.Enable(pipelines, new FormsAuthenticationConfiguration()
            {
                RedirectUrl = "~" + _element.GetElementProperty("LOGIN_PATH"),
                UserMapper = container.Resolve<IUserMapper>()
            });
        }
    }
    #endregion
    #endregion

#if NETCOREAPP2_0
    private IAssemblyCatalog assemblyCatalog;
    protected override IAssemblyCatalog AssemblyCatalog
    {
        get
        {
            
            //Console.WriteLine("Assembly name : " + ZenWebServer.ZenWebServer.assembly.FullName);
            return null;
        }
    }
#endif
}

#if NETCOREAPP2_0
/// <summary>
/// Default implementation of the <see cref="IAssemblyCatalog"/> interface, based on
/// retrieving <see cref="Assembly"/> information from <see cref="DependencyContext"/>.
/// </summary>
public class DependencyContextAssemblyCatalog : IAssemblyCatalog
{
    private static readonly Assembly NancyAssembly = typeof(INancyEngine).GetTypeInfo().Assembly;
    private readonly DependencyContext dependencyContext;

    

    /// <summary>
    /// Initializes a new instance of the <see cref="DependencyContextAssemblyCatalog"/> class,
    /// using <paramref name="entryAssembly"/>.
    /// </summary>
    public DependencyContextAssemblyCatalog(Assembly entryAssembly)
    {
        this.dependencyContext = DependencyContext.Load(entryAssembly);
        Console.WriteLine("Now in get assemblies..." + (this.dependencyContext == null));
    }

    /// <summary>
    /// Gets all <see cref="Assembly"/> instances in the catalog.
    /// </summary>
    /// <returns>An <see cref="IReadOnlyCollection{T}"/> of <see cref="Assembly"/> instances.</returns>
    public IReadOnlyCollection<Assembly> GetAssemblies()
    {
        /*
        var results = new HashSet<Assembly>
            {
                typeof (DependencyContextAssemblyCatalog).GetTypeInfo().Assembly
                typeof((Nancy.Diagnostics.DefaultDiagnostics).GetTypeInfo().Assembly)
            };
            */
       var results = new HashSet<Assembly>();
        results.Add(typeof(DependencyContextAssemblyCatalog).GetTypeInfo().Assembly);
        results.Add(typeof(Nancy.Diagnostics.DefaultDiagnostics).GetTypeInfo().Assembly);
       
         foreach (var library in this.dependencyContext.RuntimeLibraries)
         {

             if (IsReferencingNancy(library))
             {
                 foreach (var assemblyName in library.GetDefaultAssemblyNames(this.dependencyContext))
                 {
                     results.Add(SafeLoadAssembly(assemblyName));
                 }
             }
         }
        return results.ToArray();
    }

    private static Assembly SafeLoadAssembly(AssemblyName assemblyName)
    {
        try
        {
            return Assembly.Load(assemblyName);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool IsReferencingNancy(Library library)
    {
        return library.Dependencies.Any(dependency => dependency.Name.Equals(NancyAssembly.GetName().Name));
    }
}
#endif