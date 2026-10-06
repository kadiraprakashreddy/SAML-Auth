using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SmalAuth.Saml
{
    public class SamlAuthorizeAttribute : AuthorizeAttribute
    {
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            if (filterContext == null)
            {
                throw new ArgumentNullException("filterContext");
            }

            if (AllowsAnonymous(filterContext) || IsSamlEndpoint(filterContext))
            {
                return;
            }

            base.OnAuthorization(filterContext);
        }

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

            var signIn = VirtualPathUtility.ToAbsolute("~/Saml2/SignIn")
                + "?ReturnUrl=" + Uri.EscapeDataString(returnUrl);

            filterContext.Result = new RedirectResult(signIn);
        }

        private static bool AllowsAnonymous(AuthorizationContext filterContext)
        {
            var actionAllowsAnonymous = filterContext.ActionDescriptor
                .GetCustomAttributes(typeof(AllowAnonymousAttribute), true)
                .Any();

            var controllerAllowsAnonymous = filterContext.ActionDescriptor
                .ControllerDescriptor
                .GetCustomAttributes(typeof(AllowAnonymousAttribute), true)
                .Any();

            return actionAllowsAnonymous || controllerAllowsAnonymous;
        }

        private static bool IsSamlEndpoint(AuthorizationContext filterContext)
        {
            var controller = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName;
            if (string.Equals(controller, "Saml2", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var path = filterContext.HttpContext.Request.Path;
            return path != null
                && path.StartsWith("/Saml2", StringComparison.OrdinalIgnoreCase);
        }
    }
}
