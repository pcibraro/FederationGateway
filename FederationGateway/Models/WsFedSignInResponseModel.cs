using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FederationGateway.Models
{
    public class WsFedSignInResponseModel
    {
        public string Action { get; set; }

        public string WResult { get; set; }

        public string Wctx { get; set; }
    }
}
