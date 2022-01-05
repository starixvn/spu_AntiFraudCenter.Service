namespace Fanex.NAP.SPU.AFC.JobService.Controllers
{
    using Fanex.NAP.SPU.CTS.CC.JobService.Services;
    using Microsoft.AspNetCore.Mvc;
    using Sentry;
    using System.Threading.Tasks;

    [ApiController]
    [Route("api/job")]
    public class JobController : ControllerBase
    {
        private readonly ISentryClient sentryClient;
        private readonly ISyncAFCJobService syncAFCJobService;

        public JobController(ISentryClient sentryClient, ISyncAFCJobService syncAFCJobService)
        {
            this.sentryClient = sentryClient;
            this.syncAFCJobService = syncAFCJobService;
        }

        [HttpPost("SyncB2CAccount")]
        public async Task<bool> SyncAFCData()
        {
            return await this.syncAFCJobService.Execute();
        }
    }
}