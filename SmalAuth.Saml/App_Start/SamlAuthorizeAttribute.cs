using System;
using System.Web;
using System.Web.Mvc;

namespace SmalAuth.Saml
{
    public class SamlAuthorizeAttribute : AuthorizeAttribute
    {
        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (!SamlSettings.IsConfigured)
            {
                filterContext.Result = new RedirectResult("~/Home/Setup");
                return;
            }

            var request = filterContext.HttpContext.Request;
            var returnUrl = request.Url != null ? request.Url.PathAndQuery : "/";

            if (returnUrl.StartsWith("/Saml2", StringComparison.OrdinalIgnoreCase))
            {
                returnUrl = "/";
            }

            // Sustainsys sends ReturnUrl as SAML RelayState, then returns here after ACS.
            var signIn = VirtualPathUtility.ToAbsolute("~/Saml2/SignIn")
                + "?ReturnUrl=" + Uri.EscapeDataString(returnUrl);

            filterContext.Result = new RedirectResult(signIn);
        }
    }
}
