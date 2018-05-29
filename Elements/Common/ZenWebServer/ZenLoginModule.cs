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