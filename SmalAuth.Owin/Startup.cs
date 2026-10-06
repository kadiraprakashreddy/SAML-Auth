using System;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Owin;
using Sustainsys.Saml2.Owin;

[assembly: OwinStartup(typeof(SmalAuth.Owin.Startup))]

namespace SmalAuth.Owin
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = CookieAuthenticationDefaults.AuthenticationType,
                AuthenticationMode = AuthenticationMode.Active,
                LoginPath = PathString.Empty,
                CookieName = "SmalAuth.Owin.Saml",
                CookieHttpOnly = true,
                CookieSecure = CookieSecureOption.Always,
                CookieSameSite = SameSiteMode.None,
                ExpireTimeSpan = TimeSpan.FromHours(8),
                SlidingExpiration = true
            });

            app.UseSaml2Authentication(new Saml2AuthenticationOptions(true)
            {
                AuthenticationType = "Saml2",
                AuthenticationMode = AuthenticationMode.Passive,
                Caption = "Entra ID",
                SignInAsAuthenticationType = CookieAuthenticationDefaults.AuthenticationType
            });
        }
    }
}
