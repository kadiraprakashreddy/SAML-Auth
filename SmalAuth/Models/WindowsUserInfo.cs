using System.Collections.Generic;

namespace SmalAuth.Models
{
    public class WindowsUserInfo
    {
        public string Name { get; set; }
        public string AuthenticationType { get; set; }
        public string Protocol { get; set; }
        public bool IsAuthenticated { get; set; }
        public bool IsWindowsIdentity { get; set; }
        public string ImpersonationLevel { get; set; }
        public IList<string> Groups { get; set; }
    }
}
