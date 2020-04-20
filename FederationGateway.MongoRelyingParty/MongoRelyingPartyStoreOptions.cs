using System;
using System.Collections.Generic;
using System.Text;

namespace FederationGateway.MongoRelyingParty
{
    public class MongoRelyingPartyStoreOptions
    {
        public string ConnectionString { get; set; }

        public string Database { get; set; }

        public string Collection { get; set; }
    }
}
