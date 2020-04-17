using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using FederationGateway.Providers.Profiles;

namespace FederationGateway.Core.Profiles
{
    public class DefaultProfileManager : IProfileManager
    {
        public Task<ClaimsIdentity> GetProfileAsync(SignInRequest request)
        {
            if(!request.User.Identity.IsAuthenticated)
            {
                throw new SignInException("The user is not authenticated");
            }

            return Task.FromResult(request.User.Identities.First());
        }
    }
}
