namespace Fanex.NAP.SPU.CTS.CC.JobService.Services
{
    using Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData;
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

    public interface ITriggerSyncAccountService
    {
        Task<bool> InsertAccount(string json);
    }

    public class TriggerSyncAccountService : ITriggerSyncAccountService
    {
        private readonly string serviceName;
        private readonly ISentryClient sentryClient;
        private readonly ITriggerSyncAFCServices triggerSyncAFCServices;
        private readonly ISyncAFCServices syncAFCServices;
        private readonly IErrorHandleServices errorHandleServices;
        private readonly IAppSettings appSettings;

        public TriggerSyncAccountService(
            ISentryClient sentryClient,
            ITriggerSyncAFCServices triggerSyncAFCServices,
            ISyncAFCServices syncAFCServices,
        IErrorHandleServices errorHandleServices,
            IAppSettings appSettings)
        {
            this.sentryClient = sentryClient;
            this.triggerSyncAFCServices = triggerSyncAFCServices;
            this.syncAFCServices = syncAFCServices;
            this.errorHandleServices = errorHandleServices;
            this.serviceName = "Service AntiFraudCenter";
            this.appSettings = appSettings;
        }

        public Task<bool> InsertAccount(string json)
        {
            try
            {
                Process(json);
            }
            catch (Exception ex)
            {
                Utilities.LogSentryError(serviceName, ex.Message, string.Empty, ex, sentryClient);

                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        #region Private Methods

        private void Process(string json)
        {
            try
            {
                if (!string.IsNullOrEmpty(json))
                {
                   var accounts = JsonConvert.DeserializeObject<IEnumerable<AccountViewModel>>(json);

                    if(accounts != null & accounts.Any())
                    {
                        InsertAccounts(accounts);
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.LogSentryError(serviceName, "Manual Insert/Update AFC Accounts fail", json, ex, sentryClient);
                errorHandleServices.InsertErrorLog(null, ex.Message, json);
            }
        }

        public void InsertAccounts(IEnumerable<AccountViewModel> custToSync)
        {
            try
            {
                if (custToSync != null && custToSync.Any())
                {
                    var accountChunk = custToSync.Chunk(appSettings.BatchSize);

                    foreach (var accounts in accountChunk)
                    {
                        var fraudCases = triggerSyncAFCServices.GetFraudCases(accounts.Select(c=>c.CustId));

                        if (fraudCases != null && fraudCases.Any())
                        {
                            var accountInfo = triggerSyncAFCServices.GetAccountInfo(accounts, fraudCases);

                            if (accountInfo != null && accountInfo.Any())
                            {
                                var internalParticipantId = syncAFCServices.GetInternalParticipant(appSettings.AFCInternalParticipantKey);

                                var notExistedAccounts = syncAFCServices.GetAccountNotExist(accountInfo.Select(c => c.AccountID), internalParticipantId);

                                syncAFCServices.SyncAccounts(accountInfo, notExistedAccounts, internalParticipantId);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.LogSentryError(serviceName, "Insert/Update Accounts Manually Fail", custToSync, ex, sentryClient);
                errorHandleServices.InsertErrorLog(null, ex.Message, JsonConvert.SerializeObject(custToSync));
            }
        }
    }

    #endregion Private Methods
}