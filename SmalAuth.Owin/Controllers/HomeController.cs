using System.Linq;
using System.Security.Claims;
using System.Web.Mvc;
using SmalAuth.Owin.Models;

namespace SmalAuth.Owin.Controllers
{
    public class HomeController : Controller
    {
        [AllowAnonymous]
        public ActionResult Setup()
        {
            return View();
        }

        public ActionResult Index()
        {
            var identity = User.Identity as ClaimsIdentity;
            var claims = identity == null
                ? Enumerable.Empty<Claim>()
                : identity.Claims;

            var model = new SamlUserInfo
            {
                Name = User.Identity.Name,
                AuthenticationType = User.Identity.AuthenticationType,
                NameId = FirstClaim(claims,
                    ClaimTypes.NameIdentifier,
                    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"),
                AzureId = FirstClaim(claims,
                    "http://schemas.microsoft.com/identity/claims/objectidentifier",
                    "oid"),
                Email = FirstClaim(claims,
                    ClaimTypes.Email,
                    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress",
                    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"),
                Claims = claims
                    .Select(c => new SamlClaim { Type = c.Type, Value = c.Value })
                    .ToList()
            };

            return View(model);
        }

        private static string FirstClaim(System.Collections.Generic.IEnumerable<Claim> claims, params string[] types)
        {
            foreach (var type in types)
            {
                var match = claims.FirstOrDefault(c => c.Type == type);
                if (match != null && !string.IsNullOrWhiteSpace(match.Value))
                {
                    return match.Value;
                }
            }

            return null;
        }
    }
}
