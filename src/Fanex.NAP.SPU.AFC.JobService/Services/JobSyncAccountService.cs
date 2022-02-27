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
    using static Fanex.NAP.SPU.AFC.Shares.Consts;

    public interface ISyncAccountJobService
    {
        Task<bool> Execute();
    }

    public class JobSyncAccountService : ISyncAccountJobService
    {
        private readonly string serviceName;
        private readonly ISentryClient sentryClient;
        private readonly ISyncAFCServices syncAFCServices;
        private readonly IErrorHandleServices errorHandleServices;
        private readonly IAppSettings appSettings;

        public JobSyncAccountService(
            ISentryClient sentryClient,
            ISyncAFCServices syncAFCServices,
            IErrorHandleServices errorHandleServices,
            IAppSettings appSettings)
        {
            this.sentryClient = sentryClient;
            this.syncAFCServices = syncAFCServices;
            this.errorHandleServices = errorHandleServices;
            this.serviceName = "AntiFraudCenter Jobs";
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

                JobRun();
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
                        var accountInfo = syncAFCServices.GetAccount(custs, appSettings.AFCGetAccountURL, FromFlow.AFCSerivce);

                        if (accountInfo != null && accountInfo.Any())
                        {
                            var internalParticipantId = syncAFCServices.GetInternalParticipant(appSettings.AFCInternalParticipantKey);

                            var notExistedAccounts = syncAFCServices.GetAccountNotExist(accountInfo.Select(c => c.B2CCustId), internalParticipantId);

                            syncAFCServices.SyncAccounts(accountInfo, notExistedAccounts, internalParticipantId, FromFlow.AFCSerivce);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.LogSentryError(serviceName, $" {FromFlow.AFCSerivce} - Insert/Update Accounts fail", custToCheck?.Select(c=>c.CustID), ex, sentryClient, "CustIds");
                errorHandleServices.InsertErrorLog(null, $" {FromFlow.AFCSerivce} - {ex.Message}", JsonConvert.SerializeObject(custToCheck?.Select(c=>c.CustID)));
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

                            var custToDelete = syncAFCServices.GetAccountToDelete(accounts.Select(c => c.B2CCustId), internalParticipantId);

                            syncAFCServices.DeleteAccounts(custToDelete, internalParticipantId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.LogSentryError(serviceName, $" {FromFlow.AFCSerivce} - Delete Accounts fail", custToCheck?.Select(c=>c.CustID), ex, sentryClient, "CustIds");
                errorHandleServices.InsertErrorLog(null, $" {FromFlow.AFCSerivce} - {ex.Message}", JsonConvert.SerializeObject(custToCheck?.Select(c=>c.CustID)));
            }
        }
    }

    #endregion Private Methods
}