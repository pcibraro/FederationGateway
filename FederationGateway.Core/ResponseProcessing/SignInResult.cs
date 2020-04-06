using Microsoft.IdentityModel.Tokens;

namespace FederationGateway.Core.ResponseProcessing
{
    public class SignInResult
    {
        public string Realm { get; set; }
        
        public SecurityToken Token { get; set; }

        public string ReplyUrl { get; set; }
       
    }
}
