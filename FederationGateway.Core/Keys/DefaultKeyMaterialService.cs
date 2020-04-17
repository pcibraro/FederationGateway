using FederationGateway.Providers.Keys;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace FederationGateway.Core.Keys
{
    public class DefaultKeyMaterialService : IKeyMaterialService
    {
        private readonly SigningCredentials _credentials;

        public DefaultKeyMaterialService(X509Certificate2 signingCert)
        {
            if (signingCert == null) throw new ArgumentNullException(nameof(signingCert));

            var key = new X509SecurityKey(signingCert);

            _credentials = new SigningCredentials(key, 
                SecurityAlgorithms.RsaSha256Signature, 
                SecurityAlgorithms.Sha256Digest);
            
        }

        public Task<SigningCredentials> GetSigningCredentialsAsync()
        {
            return Task.FromResult(_credentials);
        }
    }
}
