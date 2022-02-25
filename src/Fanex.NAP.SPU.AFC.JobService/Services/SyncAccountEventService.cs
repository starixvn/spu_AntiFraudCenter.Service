namespace Fanex.NAP.SPU.AFC.JobService.Services
{
    using Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData;
    using Fanex.NAP.SPU.AFC.JobService;
    using Fanex.NAP.SPU.AFC.Services.SyncAFCData;
    using Fanex.NAP.SPU.AFC.Shares;
    using Newtonsoft.Json;
    using Sentry;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public interface ISyncAccountEventService
    {
        Task<bool> Execute();
    }

    public class SyncAccountEventService : ISyncAccountEventService
    {
        private readonly string serviceName;
        private readonly ISentryClient sentryClient;
        private readonly ISyncAFCServices syncAFCServices;
        private readonly IErrorHandleServices errorHandleServices;
        private readonly IAppSettings appSettings;

        public SyncAccountEventService(
            ISentryClient sentryClient,
            ISyncAFCServices syncAFCServices,
            IErrorHandleServices errorHandleServices,
            IAppSettings appSettings)
        {
            this.sentryClient = sentryClient;
            this.syncAFCServices = syncAFCServices;
            this.errorHandleServices = errorHandleServices;
            this.serviceName = "Service AntiFraudCenter";
            this.appSettings = appSettings;
        }

        public Task<bool> Execute()
        {
            try
            {
                Process();
            }
            catch (Exception ex)
            {
                Utilities.LogSentryError(serviceName, ex.Message, string.Empty, ex, sentryClient);

                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        #region Private Methods

        private void Process()
        {
            var custToCheck = syncAFCServices.GetCustChangedCategory();

            if (custToCheck != null && custToCheck.Any())
            {
                Parallel.Invoke(() =>
                {
                    var accToInsert = custToCheck.Where(c => c.FraudID != null && c.FraudID.Any());

                    InsertAccounts(accToInsert, appSettings.BatchSize);
                },
                () =>
                {
                    var accToDelete = custToCheck.Where(c => c.FraudID == null || !c.FraudID.Any());

                    DeleteAccounts(accToDelete, appSettings.BatchSize);
                });

                Process();
            }
        }

        public void InsertAccounts(IEnumerable<CustToSyncAFCEntity> custToCheck, int batchSize)
        {
            try
            {
                if (custToCheck != null && custToCheck.Any())
                {
                    var custChunk = custToCheck.Chunk(batchSize);

                    foreach (var custs in custChunk)
                    {
                        var accountInfo = syncAFCServices.GetAccount(custs, appSettings.AFCGetAccountURL);

                        if (accountInfo != null && accountInfo.Any())
                        {
                            var internalParticipantId = syncAFCServices.GetInternalParticipant(appSettings.AFCInternalParticipantKey);

                            var notExistedAccounts = syncAFCServices.GetAccountNotExist(accountInfo.Select(c => c.AccountID), internalParticipantId);

                            syncAFCServices.SyncAccounts(accountInfo, notExistedAccounts, internalParticipantId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.LogSentryError(serviceName, "Insert/Updte Accounts fail", custToCheck, ex, sentryClient);
                errorHandleServices.InsertErrorLog(null, ex.Message, JsonConvert.SerializeObject(custToCheck));
            }
        }

        public void DeleteAccounts(IEnumerable<CustToSyncAFCEntity> custToCheck, int batchSize)
        {
            try
            {
                if (custToCheck != null && custToCheck.Any())
                {
                    var custChunk = custToCheck.Chunk(batchSize);

                    foreach (var custs in custChunk)
                    {
                        var accounts = syncAFCServices.GetB2CAccountId(custs);

                        if (accounts != null && accounts.Any())
                        {
                            var internalParticipantId = syncAFCServices.GetInternalParticipant(appSettings.AFCInternalParticipantKey);

                            var b2cAccountIds = accounts.Select(c => c.B2CCustId);

                            var notExistedAccounts = syncAFCServices.GetAccountNotExist(b2cAccountIds, internalParticipantId);

                            var custToDelete = b2cAccountIds.Except(notExistedAccounts);

                            syncAFCServices.DeleteAccounts(custToDelete, internalParticipantId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.LogSentryError(serviceName, "Delete Accounts fail", custToCheck, ex, sentryClient);
                errorHandleServices.InsertErrorLog(null, ex.Message, JsonConvert.SerializeObject(custToCheck));
            }
        }
    }

    #endregion Private Methods
}