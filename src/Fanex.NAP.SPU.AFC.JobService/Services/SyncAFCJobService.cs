namespace Fanex.NAP.SPU.CTS.CC.JobService.Services
{
    using Fanex.NAP.SPU.AFC.Features.SyncAFCData;
    using Fanex.NAP.SPU.AFC.JobService;
    using Fanex.NAP.SPU.AFC.Shares;
    using Sentry;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public interface ISyncAFCJobService
    {
        Task<bool> Execute();
    }

    public class SyncAFCJobService : ISyncAFCJobService
    {
        private readonly string serviceName;
        private readonly ISentryClient sentryClient;
        private readonly ISyncAFCServices syncAFCServices;
        private readonly IAppSettings appSettings;

        public SyncAFCJobService(
            ISentryClient sentryClient,
            ISyncAFCServices syncAFCServices,
            IAppSettings appSettings)
        {
            this.sentryClient = sentryClient;
            this.syncAFCServices = syncAFCServices;
            this.serviceName = "Service AntiFraudCenter";
            this.appSettings = appSettings;
        }

        public Task<bool> Execute()
        {
            try
            {
                JobRun();
            }
            catch (Exception ex)
            {
                Utilities.LogSentryError(serviceName, ex.Message, string.Empty, ex, sentryClient);

                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        #region Private Methods

        private void JobRun()
        {
            var custToCheck = syncAFCServices.GetCustomerToSyncAFC();

            if (custToCheck != null && custToCheck.Any())
            {
                Parallel.Invoke(() =>
                {
                    var accToInsert = custToCheck.Where(c => c.FraudID != null && c.FraudID.Any());

                    syncAFCServices.InsertAFCAccountsAsync(accToInsert, appSettings.AFCInsertAccountsAPI, appSettings.AFCUpdateAccountsAPI);
                },
                () =>
                {
                    var accToDelete = custToCheck.Where(c => c.FraudID == null || !c.FraudID.Any());

                    syncAFCServices.DeleteAFCAccountsAsync(accToDelete.Select(c => c.CustID).Distinct(), appSettings.AFCDeleteAccountsAPI);
                });

                JobRun();
            }
        }
    }

    #endregion Private Methods
}