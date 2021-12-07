namespace Fanex.NAP.SPU.AFC.JobService.Services
{
    using Fanex.NAP.Client.AspNetCore.Session;
    using Fanex.NAP.Client.Helpers.Web;
    using Fanex.NAP.Client.Services;
    using Fanex.NAP.SPU.AFC.Features.SyncAFCData;
    using Fanex.NAP.SPU.CTS.CC.JobService.Services;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;

    public static class InitService
    {
        public static void RegisterService(this IServiceCollection services)
        {
            RegisterNAPService(services);
            RegisterAFCService(services);
        }

        private static void RegisterNAPService(this IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<INapSession, NapSession>();
            services.AddSingleton<ISecurityService, SecurityService>();
        }

        private static void RegisterAFCService(this IServiceCollection services)
        {
            services.AddSingleton<ISyncAFCDataAccess, SyncAFCDataAccess>();
            services.AddSingleton<ISyncAFCServices, SyncAFCServices>();
            services.AddSingleton<ISyncAFCJobService, SyncAFCJobService>();
        }
    }
}