namespace Fanex.NAP.SPU.AFC.JobService.Services
{
    using Fanex.NAP.SPU.AFC.JobService;
    using Fanex.NAP.SPU.AFC.Services.SyncAFCData;
    using Fanex.NAP.SPU.AFC.Services.TriggerSyncAFC;
    using Fanex.NAP.SPU.AFC.Shares;
    using Newtonsoft.Json;
    using Sentry;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using static Fanex.NAP.SPU.AFC.Shares.Consts;

    public interface IEventSyncAccountService
    {
        Task<bool> InsertProcess(IEnumerable<Int64> custIds, FromFlow fromFlow);

        Task<bool> DeleteProcess(IEnumerable<Int64> custIds, FromFlow fromFlow);
    }

    public class EventSyncAccountService : IEventSyncAccountService
    {
        private readonly string serviceName;
        private readonly ISentryClient sentryClient;
        private readonly ISyncAFCServices syncAFCServices;   
        private readonly ITriggerSyncAFCServices triggerSyncAFCServices;
        private readonly IErrorHandleServices errorHandleServices;
        private readonly IAppSettings appSettings;

        public EventSyncAccountService(
            ISentryClient sentryClient,
            ISyncAFCServices syncAFCServices,
            ITriggerSyncAFCServices triggerSyncAFCServices,
            IErrorHandleServices errorHandleServices,
            IAppSettings appSettings)
        {
            this.sentryClient = sentryClient;
            this.syncAFCServices = syncAFCServices;
            this.triggerSyncAFCServices = triggerSyncAFCServices;
            this.errorHandleServices = errorHandleServices;
            this.serviceName = "AntiFraudCenter Events";
            this.appSettings = appSettings;
        }

        public Task<bool> InsertProcess(IEnumerable<Int64> custIds, FromFlow fromFlow)
        {
            try
            {
                InsertAccounts(custIds, fromFlow);
            }
            catch (Exception ex)
            {
                Utilities.LogSentryError(serviceName, $" {fromFlow} - Insert/Updte Accounts fail", custIds, ex, sentryClient, "CustIds");
                errorHandleServices.InsertErrorLog(null, ex.Message, JsonConvert.SerializeObject(custIds));

                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        public Task<bool> DeleteProcess(IEnumerable<Int64> custIds, FromFlow fromFlow)
        {
            try
            {
                DeleteAccounts(custIds);
            }
            catch (Exception ex)
            {
                Utilities.LogSentryError(serviceName, $" {fromFlow} - Delete Accounts fail", custIds, ex, sentryClient, "CustIds");
                errorHandleServices.InsertErrorLog(null, ex.Message, JsonConvert.SerializeObject(custIds));

                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        #region Private Methods

        public void InsertAccounts(IEnumerable<Int64> custIds, FromFlow fromFlow)
        {
            if (custIds != null && custIds.Any())
            {
                var custChunk = custIds.Chunk(appSettings.BatchSize);

                foreach (var custs in custChunk)
                {
                    var fraudCases = triggerSyncAFCServices.GetFraudCases(custs);

                    if (fraudCases != null && fraudCases.Any())
                    {
                        var custIdsToInsert = fraudCases.Where(c => c.FraudIDList != null && c.FraudIDList.Any());

                        var accountInfo = syncAFCServices.GetAccount(custIdsToInsert, appSettings.AFCGetAccountURL, fromFlow);

                        if (accountInfo != null && accountInfo.Any())
                        {
                            var internalParticipantId = syncAFCServices.GetInternalParticipant(appSettings.AFCInternalParticipantKey);

                            var notExistedAccounts = syncAFCServices.GetAccountNotExist(accountInfo.Select(c => c.B2CCustId), internalParticipantId);

                            syncAFCServices.SyncAccounts(accountInfo, notExistedAccounts, internalParticipantId, FromFlow.AFCSerivce);
                        }
                    }    

                  
                }
            }
        }

        public void DeleteAccounts(IEnumerable<Int64> custIds)
        {
            if (custIds != null && custIds.Any())
            {
                var custChunk = custIds.Chunk(appSettings.BatchSize);

                foreach (var custs in custChunk)
                {
                    var fraudCases = triggerSyncAFCServices.GetFraudCases(custs);

                    var custIdsToDelete = fraudCases.Where(c => c.FraudIDList == null || !c.FraudIDList.Any());

                    var b2cCustIds = syncAFCServices.GetB2CAccountId(custIdsToDelete);

                    if (b2cCustIds != null && b2cCustIds.Any())
                    {
                        var internalParticipantId = syncAFCServices.GetInternalParticipant(appSettings.AFCInternalParticipantKey);

                        var custToDelete = syncAFCServices.GetAccountToDelete(b2cCustIds.Select(c => c.B2CCustId), internalParticipantId);
                        
                        syncAFCServices.DeleteAccounts(custToDelete, internalParticipantId);
                    }
                }
            }
        }
    }

    #endregion Private Methods
}