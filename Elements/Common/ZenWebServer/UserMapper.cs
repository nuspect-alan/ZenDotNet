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
