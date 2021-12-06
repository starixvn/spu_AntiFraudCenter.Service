namespace Fanex.NAP.SPU.CTS.CC.JobService.Services
{
    using Fanex.NAP.SPU.AFC.Features.SyncAFCData;
    using Fanex.NAP.SPU.AFC.Shares;
    using Sentry;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public interface ISyncAFCJobService
    {
        Task<bool> Execute(int batchSize);
    }

    public class SyncAFCJobService : ISyncAFCJobService
    {
        private readonly string serviceName;
        private readonly ISentryClient sentryClient;
        private readonly ISyncAFCServices syncAFCServices;

        public SyncAFCJobService(
            ISentryClient sentryClient,
            ISyncAFCServices syncAFCServices)
        {
            this.sentryClient = sentryClient;
            this.syncAFCServices = syncAFCServices;
            this.serviceName = "SyncAFC";
        }

        public Task<bool> Execute(int batchSize)
        {
            try
            {
                JobRun(batchSize);
            }
            catch (Exception ex)
            {
                Utilities.LogSentryError(serviceName, ex.Message, string.Empty, ex, sentryClient);

                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        #region Private Methods

        private void JobRun(int batchSize)
        {
            var customerToSyncAFC = syncAFCServices.GetCustomerToSyncAFC();

            if (customerToSyncAFC != null && customerToSyncAFC.Any())
            {
                var custToString = string.Join(",",customerToSyncAFC.Select(c => c.CustID).Distinct());

                var custB2CInfo = syncAFCServices.GetB2CInfo(custToString);

                ///TODO:
                ///

                JobRun(batchSize);
            }
        }
    }

    #endregion Private Methods
}