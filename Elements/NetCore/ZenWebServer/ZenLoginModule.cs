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
#if !NETCOREAPP2_0
using System;
using System.Dynamic;
using Nancy.Authentication.Forms;
using Nancy.Extensions;
using Nancy;
using ZenCommon;

public class ZenLoginModule : NancyModule
{
    public ZenLoginModule(IElement element)
    {
        #region Gets
        ////Get("/", args => {
        ////    return View["index"/*element.GetElementProperty("INDEX_PAGE")*/];
        ////});

        Get(element.GetElementProperty("LOGIN_PATH"), args =>
        {
            dynamic model = new ExpandoObject();
            model.Errored = this.Request.Query.error.HasValue;

            return View[element.GetElementProperty("LOGIN_VIEW_NAME"), model];
        });

        Get(element.GetElementProperty("LOGOUT_PATH"), args =>
        {
            return this.LogoutAndRedirect(element.GetElementProperty("LOGOUT_REDIRECTED_URL"));
        });
        #endregion

        #region Posts
        Post(element.GetElementProperty("LOGIN_PATH"), args =>
        {
            var userGuid = UserMapper.ValidateUser((string)this.Request.Form.Username, (string)this.Request.Form.Password);

            if (userGuid == null)
                return this.Context.GetRedirect("~" + element.GetElementProperty("LOGIN_PATH") + "?error=true&username=" + (string)this.Request.Form.Username);

            DateTime? expiry = null;
            if (this.Request.Form.RememberMe.HasValue)
                expiry = DateTime.Now.AddDays(7);

            return this.LoginAndRedirect(userGuid.Value, expiry, element.GetElementProperty("LOGIN_REDIRECTED_URL"));
        });
        #endregion
    }
}
#endif