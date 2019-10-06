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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Security.Principal;

public class UserMapper : IUserMapper
{
    #region Fields
    #region _users
    static List<Tuple<string, string, Guid, DateTime>> _users = new List<Tuple<string, string, Guid, DateTime>>();
    #endregion

    #region _compiledAssembly
    public static Assembly _compiledAssembly;
    #endregion
    #endregion

    #region Functions
    #region GetUserFromIdentifier
    public ClaimsPrincipal GetUserFromIdentifier(Guid identifier, NancyContext context)
    {
        var userRecord = _users.FirstOrDefault(u => u.Item3 == identifier);

        return userRecord == null ? null : new ClaimsPrincipal(new GenericIdentity(userRecord.Item1));
    }
    #endregion

    #region ValidateUser
    public static Guid? ValidateUser(string username, string password)
    {
        MethodInfo authMethod = _compiledAssembly.GetType("ZenAuthentication").GetMethod("Authenticate");
        Guid? res = ((Guid?)authMethod.Invoke(null, new object[] { username, password }));
        if (res.HasValue)
        {
            _users.Add(new Tuple<string, string, Guid, DateTime>(username, password, res.Value, DateTime.Now));
            return res.Value;
        }
        else
            return null;
    }
    #endregion
    #endregion
}
#endif