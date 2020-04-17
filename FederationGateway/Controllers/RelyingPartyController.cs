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
        public async Task<IActionResult> Get()
        {
            var relyingParties = await _relyingPartyStore.GetAll();
            return Ok(relyingParties);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var relyingParty = await _relyingPartyStore.GetById(id);

            return Ok(relyingParty);

        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            await _relyingPartyStore.Delete(id);

            return Ok();
        }

        [HttpPut]
        [Route("{id}")]
        public async Task<IActionResult> Update(RelyingParty relyingParty)
        {
            var updated = await _relyingPartyStore.Update(relyingParty);

            return Ok(updated);
        }

        [HttpPost]
        public async Task<IActionResult> Create(RelyingParty relyingParty)
        {
            var updated = await _relyingPartyStore.Create(relyingParty);
            
            return Ok(updated);
        }


    }
}