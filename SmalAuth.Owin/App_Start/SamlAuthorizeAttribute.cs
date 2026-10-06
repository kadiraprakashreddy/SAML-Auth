using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security;

namespace SmalAuth.Owin
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

            filterContext.Result = new Saml2ChallengeResult(returnUrl);
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
            var path = filterContext.HttpContext.Request.Path;
            return path != null
                && path.StartsWith("/Saml2", StringComparison.OrdinalIgnoreCase);
        }
    }

    public class Saml2ChallengeResult : HttpUnauthorizedResult
    {
        private readonly string _redirectUri;

        public Saml2ChallengeResult(string redirectUri)
        {
            _redirectUri = redirectUri;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            var properties = new AuthenticationProperties
            {
                RedirectUri = string.IsNullOrEmpty(_redirectUri) ? "/" : _redirectUri
            };

            context.HttpContext.GetOwinContext().Authentication.Challenge(properties, "Saml2");
        }
    }
}
