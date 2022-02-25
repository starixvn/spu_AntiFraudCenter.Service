namespace Fanex.NAP.SPU.AFC.JobService.Controllers
{
    using Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData;
    using Fanex.NAP.SPU.AFC.JobService.Services;
    using Microsoft.AspNetCore.Mvc;
    using Sentry;
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

        [HttpPost("ManualInsertAccount")]
        public async Task<bool> InsertAccountByJson([FromBody] IEnumerable<AccountViewModel> accounts)
        {
            return await this.triggerSyncAccountService.InsertAccount(accounts);
        }
    }
}