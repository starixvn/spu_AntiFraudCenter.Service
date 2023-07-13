namespace Fanex.NAP.SPU.AFC.JobService.Controllers
{
    using Fanex.NAP.SPU.AFC.AccountHandlerServices;
    using Fanex.NAP.SPU.AFC.JobService.Services;
    using Microsoft.AspNetCore.Mvc;
    using Sentry;
    using System.Threading.Tasks;

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

        //[HttpPost("InsertAccount")]
        //public async Task<bool> InsertAccount([FromBody] EventControllerParaModel paras)
        //{
        //    return await this.eventSyncAccountService.InsertProcess(paras);
        //}

        //[HttpPost("DeleteAccount")]
        //public async Task<bool> DeleteAccount([FromBody] EventControllerParaModel paras)
        //{
        //    return await this.eventSyncAccountService.DeleteProcess(paras);
        //}
    }
}