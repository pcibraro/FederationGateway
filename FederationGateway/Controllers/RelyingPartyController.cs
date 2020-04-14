using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FederationGateway.Core.RelyingParties;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FederationGateway.ManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RelyingPartyController : ControllerBase
    {
        private readonly ILogger<RelyingPartyController> _logger;
        private readonly IRelyingPartyStore _relyingPartyStore;

        public RelyingPartyController(ILogger<RelyingPartyController> logger, 
            IRelyingPartyStore relyingPartyStore)
        {
            _logger = logger;
            _relyingPartyStore = relyingPartyStore;
        }

        [HttpGet]
        public JsonResult Get()
        {
            var relyingParties = _relyingPartyStore.GetAll();

            return new JsonResult(relyingParties);
            
        }

        [HttpGet]
        [Route("{realm}")]
        public JsonResult Get(string realm)
        {
            var relyingParties = _relyingPartyStore.GetByRealm(realm);

            return new JsonResult(relyingParties);

        }


    }
}