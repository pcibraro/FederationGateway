using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FederationGateway.Providers.Keys
{
    public interface IKeyMaterialService
    {
        Task<SigningCredentials> GetSigningCredentialsAsync();
    }
}
