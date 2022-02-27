namespace Fanex.NAP.SPU.AFC.JobService.Controllers
{
    using Fanex.NAP.SPU.AFC.JobService.Services;
    using Microsoft.AspNetCore.Mvc;
    using Sentry;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using static Fanex.NAP.SPU.AFC.Shares.Consts;

    [ApiController]
    [Route("api/event")]
    public class EventController : ControllerBase
    {
        private readonly ISentryClient sentryClient;
        private readonly IEventSyncAccountService eventSyncAccountService;

        public EventController(ISentryClient sentryClient, IEventSyncAccountService eventSyncAccountService)
        {
            this.sentryClient = sentryClient;
            this.eventSyncAccountService = eventSyncAccountService;
        }

        [HttpPost("InsertAccount")]
        public async Task<bool> InsertAccount([FromBody] IEnumerable<Int64> custIds, [FromBody] FromFlow flowName)
        {
            return await this.eventSyncAccountService.InsertProcess(custIds, flowName);
        }

        [HttpPost("DeleteAccount")]
        public async Task<bool> DeleteAccount([FromBody] IEnumerable<Int64> custIds, [FromBody] FromFlow flowName)
        {
            return await this.eventSyncAccountService.DeleteProcess(custIds, flowName);
        }
    }
}