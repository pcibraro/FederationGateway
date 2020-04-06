using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using FederationGateway.Core.ResponseProcessing;

namespace FederationGateway.Core.Profiles
{
    public class DefaultProfileManager : IProfileManager
    {
        public Task<ClaimsIdentity> GetProfileAsync(SignInRequest request)
        {
            return Task.FromResult(request.User.Identities.First());
        }
    }
}
