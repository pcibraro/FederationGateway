using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FederationGateway.Models
{
    public class WsFedSignOutResponseModel
    {
        public List<string> LogoutUrls { get; set; }

        public string ReplyTo { get; set; }
    }
}
