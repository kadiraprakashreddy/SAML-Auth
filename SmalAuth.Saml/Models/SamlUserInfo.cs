using System.Collections.Generic;

namespace SmalAuth.Saml.Models
{
    public class SamlClaim
    {
        public string Type { get; set; }
        public string Value { get; set; }
    }

    public class SamlUserInfo
    {
        public string Name { get; set; }
        public string NameId { get; set; }
        public string AzureId { get; set; }
        public string Email { get; set; }
        public string AuthenticationType { get; set; }
        public IList<SamlClaim> Claims { get; set; }
    }
}
