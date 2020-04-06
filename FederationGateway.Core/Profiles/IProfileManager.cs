using FederationGateway.Core.ResponseProcessing;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace FederationGateway.Core.Profiles
{
    public interface IProfileManager
    {
        Task<ClaimsIdentity> GetProfileAsync(SignInRequest request);
    }
}
