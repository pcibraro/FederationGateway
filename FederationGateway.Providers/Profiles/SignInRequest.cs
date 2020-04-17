using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace FederationGateway.Providers.Profiles
{
    public class SignInRequest
    {
        public string Realm { get; set; }

        public ClaimsPrincipal User { get; set; }

        public IDictionary<string, string> Parameters { get; set; }
    }
}
