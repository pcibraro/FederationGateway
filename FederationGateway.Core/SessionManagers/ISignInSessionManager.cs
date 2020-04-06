using System;
using System.Collections.Generic;
using System.Text;

namespace FederationGateway.Core.SessionManagers
{
    public interface ISignInSessionManager
    {
        void AddRealm(string realm);

        IEnumerable<string> GetRealms();

        void ClearEndpoints();
    }
}
