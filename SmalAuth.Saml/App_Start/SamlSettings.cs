using System;
using System.Linq;
using Sustainsys.Saml2.Configuration;

namespace SmalAuth.Saml
{
    public static class SamlSettings
    {
        private static SustainsysSaml2Section Config
        {
            get { return SustainsysSaml2Section.Current; }
        }

        private static IdentityProviderElement IdentityProvider
        {
            get
            {
                return Config.IdentityProviders
                    .OfType<IdentityProviderElement>()
                    .FirstOrDefault();
            }
        }

        public static bool IsConfigured
        {
            get
            {
                var metadata = MetadataUrl;
                return !string.IsNullOrWhiteSpace(SpEntityId)
                    && !string.IsNullOrWhiteSpace(metadata)
                    && metadata.IndexOf("YOUR-TENANT-ID", StringComparison.OrdinalIgnoreCase) < 0;
            }
        }

        public static string TenantId
        {
            get
            {
                var entityId = IdentityProvider == null ? null : IdentityProvider.EntityId;
                const string prefix = "https://sts.windows.net/";
                if (string.IsNullOrEmpty(entityId))
                {
                    return null;
                }

                var start = entityId.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
                if (start < 0)
                {
                    return entityId;
                }

                return entityId.Substring(start + prefix.Length).Trim('/');
            }
        }

        public static string SpEntityId
        {
            get
            {
                return Config.EntityId != null ? Config.EntityId.Id : null;
            }
        }

        public static string AcsUrl
        {
            get
            {
                var origin = Config.PublicOrigin != null
                    ? Config.PublicOrigin.ToString().TrimEnd('/')
                    : string.Empty;
                var modulePath = string.IsNullOrEmpty(Config.ModulePath)
                    ? "/Saml2"
                    : Config.ModulePath;

                if (!modulePath.StartsWith("/", StringComparison.Ordinal))
                {
                    modulePath = "/" + modulePath;
                }

                return origin + modulePath + "/Acs";
            }
        }

        public static string MetadataUrl
        {
            get
            {
                return IdentityProvider == null ? null : IdentityProvider.MetadataLocation;
            }
        }
    }
}
