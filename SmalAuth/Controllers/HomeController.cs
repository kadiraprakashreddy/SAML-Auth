using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web.Mvc;
using SmalAuth.Models;

namespace SmalAuth.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var identity = User.Identity;
            var windowsIdentity = identity as WindowsIdentity;
            var groups = new List<string>();

            if (windowsIdentity != null && windowsIdentity.Groups != null)
            {
                foreach (var group in windowsIdentity.Groups)
                {
                    try
                    {
                        groups.Add(group.Translate(typeof(NTAccount)).Value);
                    }
                    catch (IdentityNotMappedException)
                    {
                        groups.Add(group.Value);
                    }
                }
            }

            var model = new WindowsUserInfo
            {
                Name = identity.Name,
                AuthenticationType = identity.AuthenticationType,
                Protocol = ResolveProtocol(identity.AuthenticationType),
                IsAuthenticated = identity.IsAuthenticated,
                IsWindowsIdentity = windowsIdentity != null,
                ImpersonationLevel = windowsIdentity != null
                    ? windowsIdentity.ImpersonationLevel.ToString()
                    : null,
                Groups = groups.OrderBy(g => g).ToList()
            };

            return View(model);
        }

        private static string ResolveProtocol(string authenticationType)
        {
            if (string.Equals(authenticationType, "Kerberos", StringComparison.OrdinalIgnoreCase))
            {
                return "Kerberos";
            }

            if (string.Equals(authenticationType, "NTLM", StringComparison.OrdinalIgnoreCase))
            {
                return "NTLM";
            }

            if (string.Equals(authenticationType, "Negotiate", StringComparison.OrdinalIgnoreCase))
            {
                return "Negotiate (Kerberos preferred, NTLM fallback)";
            }

            return string.IsNullOrEmpty(authenticationType) ? "(unknown)" : authenticationType;
        }
    }
}
