namespace Fanex.NAP.SPU.AFC.JobService.Controllers
{
    using Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData;
    using Fanex.NAP.SPU.AFC.JobService.Services;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json;
    using Sentry;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    [ApiController]
    [Route("api/trigger")]
    public class TriggerController : ControllerBase
    {
        private readonly ISentryClient sentryClient;
        private readonly ITriggerSyncAccountService triggerSyncAccountService;

        public TriggerController(ISentryClient sentryClient, ITriggerSyncAccountService triggerSyncAccountService)
        {
            this.sentryClient = sentryClient;
            this.triggerSyncAccountService = triggerSyncAccountService;
        }

        //[HttpPost("ManualInsertAccount")]
        //public async Task<bool> InsertAccountByJson([FromBody] IEnumerable<AccountParameterModel> accounts)
        //{
        //    return await this.triggerSyncAccountService.InsertAccount(accounts);
        //}

        [HttpGet("health")]
        public string CheckHealth()
        {
            return JsonConvert.SerializeObject(DateTime.Now.ToString());
        }
    }
}