namespace Fanex.NAP.SPU.AFC.JobService.Services
{
    using Fanex.NAP.SPU.AFC.AccountHandlerServices;
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

    public interface IEventSyncAccountService
    {
        Task<bool> InsertProcess(EventControllerParaModel paras);

        Task<bool> DeleteProcess(EventControllerParaModel paras);
    }

    public class EventSyncAccountService : IEventSyncAccountService
    {
        private readonly string serviceName;
        private readonly ISentryClient sentryClient;
        private readonly ISyncAFCServices syncAFCServices;
        private readonly IAccountHandlerServices accountHandlerServices;
        private readonly IErrorHandleServices errorHandleServices;
        private readonly IAppSettings appSettings;

        public EventSyncAccountService(
            ISentryClient sentryClient,
            ISyncAFCServices syncAFCServices,
            IAccountHandlerServices accountHandlerServices,
            IErrorHandleServices errorHandleServices,
            IAppSettings appSettings)
        {
            this.sentryClient = sentryClient;
            this.syncAFCServices = syncAFCServices;
            this.accountHandlerServices = accountHandlerServices;
            this.errorHandleServices = errorHandleServices;
            this.serviceName = "AntiFraudCenter Events";
            this.appSettings = appSettings;
        }

        public Task<bool> InsertProcess(EventControllerParaModel paras)
        {
            try
            {
                InsertAccounts(paras.CustIds, paras.FromFlow);
            }
            catch (Exception ex)
            {
                Utilities.LogSentryError(serviceName, $" {paras.FromFlow} - Insert/Updte Accounts fail", paras.CustIds, ex, sentryClient, "CustIds");
                errorHandleServices.InsertErrorLog(null, ex.Message, JsonConvert.SerializeObject(paras.CustIds));

                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        public Task<bool> DeleteProcess(EventControllerParaModel paras)
        {
            try
            {
                DeleteAccounts(paras.CustIds);
            }
            catch (Exception ex)
            {
                Utilities.LogSentryError(serviceName, $" {paras.FromFlow} - Delete Accounts fail", paras.CustIds, ex, sentryClient, "CustIds");
                errorHandleServices.InsertErrorLog(null, ex.Message, JsonConvert.SerializeObject(paras.CustIds));

                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        #region Private Methods

        public void InsertAccounts(IEnumerable<Int64> custIds, FromFlow fromFlow)
        {
            if (custIds != null && custIds.Any())
            {
                var custChunk = custIds.Distinct().Chunk(appSettings.BatchSize);

                foreach (var custs in custChunk)
                {
                    var fraudCases = accountHandlerServices.GetFraudCases(custs);

                    if (fraudCases != null && fraudCases.Any())
                    {
                        var custIdsToInsert = fraudCases.Where(c => c.FraudIDList != null && c.FraudIDList.Any());

                        var accountInfo = syncAFCServices.GetAccount(custIdsToInsert, appSettings.AFCGetAccountURL, fromFlow);

                        if (accountInfo != null && accountInfo.Any())
                        {
                            var internalParticipantId = accountHandlerServices.GetInternalParticipant(appSettings.AFCInternalParticipantKey);

                            var notExistedAccounts = accountHandlerServices.GetAccountNotExist(accountInfo.Select(c => c.B2CCustId), internalParticipantId);

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
                var custChunk = custIds.Distinct().Chunk(appSettings.BatchSize);

                foreach (var custs in custChunk)
                {
                    var fraudCases = accountHandlerServices.GetFraudCases(custs);

                    var custIdsToDelete = fraudCases.Where(c => c.FraudIDList == null || !c.FraudIDList.Any());

                    var b2cCustIds = syncAFCServices.GetB2CCustIds(custIdsToDelete);

                    if (b2cCustIds != null && b2cCustIds.Any())
                    {
                        var internalParticipantId = accountHandlerServices.GetInternalParticipant(appSettings.AFCInternalParticipantKey);

                        var custToDelete = syncAFCServices.GetAccountToDelete(b2cCustIds.Select(c => c.B2CCustId), internalParticipantId);

                        syncAFCServices.DeleteAccounts(custToDelete, internalParticipantId);
                    }
                }
            }
        }
    }

    #endregion Private Methods
}