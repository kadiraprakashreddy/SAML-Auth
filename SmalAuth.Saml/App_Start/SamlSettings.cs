using System.Configuration;

namespace SmalAuth.Saml
{
    public static class SamlSettings
    {
        public static string TenantId
        {
            get { return ConfigurationManager.AppSettings["ida:TenantId"]; }
        }

        public static bool IsConfigured
        {
            get
            {
                var tenant = TenantId;
                return !string.IsNullOrWhiteSpace(tenant)
                    && tenant != "YOUR-TENANT-ID";
            }
        }

        public static string SpEntityId
        {
            get { return ConfigurationManager.AppSettings["saml:SpEntityId"]; }
        }

        public static string AcsUrl
        {
            get { return ConfigurationManager.AppSettings["saml:AcsUrl"]; }
        }

        public static string MetadataUrl
        {
            get { return ConfigurationManager.AppSettings["saml:MetadataUrl"]; }
        }
    }
}
