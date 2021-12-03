namespace Fanex.NAP.SPU.AFC.JobService.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Sentry;
    using System.Threading.Tasks;

    [ApiController]
    [Route("api/job")]
    public class JobController : ControllerBase
    {
        private readonly ISentryClient sentryClient;

        public JobController(ISentryClient sentryClient)
        {
            this.sentryClient = sentryClient;
        }

        [HttpPost("test")]
        public async Task<bool> test([FromForm] int size)
        {
            return true;
        }
    }
}