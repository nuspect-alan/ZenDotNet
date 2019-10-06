/*************************************************************************
 * Copyright (c) 2015, 2019 Zenodys BV
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

#if !NETCOREAPP2_0
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

    protected override void ConfigureConventions(NancyConventions nancyConventions)
     {
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
}

#endif