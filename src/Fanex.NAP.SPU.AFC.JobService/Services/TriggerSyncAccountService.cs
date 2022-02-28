namespace Fanex.NAP.SPU.AFC.JobService.Services
{
    using Fanex.NAP.SPU.AFC.AccountHandlerServices;
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

    public interface ITriggerSyncAccountService
    {
        Task<bool> InsertAccount(IEnumerable<AccountViewModel> accounts);
    }

    public class TriggerSyncAccountService : ITriggerSyncAccountService
    {
        private readonly string serviceName;
        private readonly ISentryClient sentryClient;
        private readonly IAccountHandlerServices accountHandlerServices;
        private readonly ISyncAFCServices syncAFCServices;
        private readonly IErrorHandleServices errorHandleServices;
        private readonly IAppSettings appSettings;

        public TriggerSyncAccountService(ISentryClient sentryClient,
             IAccountHandlerServices accountHandlerServices,
             ISyncAFCServices syncAFCServices,
             IErrorHandleServices errorHandleServices,
             IAppSettings appSettings)
        {
            this.sentryClient = sentryClient;
            this.accountHandlerServices = accountHandlerServices;
            this.syncAFCServices = syncAFCServices;
            this.errorHandleServices = errorHandleServices;
            this.serviceName = "Manual Insert AFCAccount";
            this.appSettings = appSettings;
        }

        public Task<bool> InsertAccount(IEnumerable<AccountViewModel> accounts)
        {
            try
            {
                if (accounts != null & accounts.Any())
                {
                    InsertAccounts(accounts);
                }
            }
            catch (Exception ex)
            {
                Utilities.LogSentryError(serviceName, $" {FromFlow.ManualTrigger} - Insert/Update AFC Accounts fail", JsonConvert.SerializeObject(accounts?.Select(c => c.CustId)), ex, sentryClient, "CustIds");
                errorHandleServices.InsertErrorLog(null, $" {FromFlow.ManualTrigger} - {ex.Message}", JsonConvert.SerializeObject(accounts?.Select(c => c.CustId)));

                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        #region Private Methods

        public void InsertAccounts(IEnumerable<AccountViewModel> custToSync)
        {
            if (custToSync != null && custToSync.Any())
            {
                var accountChunk = custToSync.Chunk(appSettings.BatchSize);

                foreach (var accounts in accountChunk)
                {
                    var fraudCases = accountHandlerServices.GetFraudCases(accounts.Select(c => c.CustId));

                    if (fraudCases != null && fraudCases.Any())
                    {
                        var accountInfo = accountHandlerServices.EncryptAccount(accounts, fraudCases);

                        if (accountInfo != null && accountInfo.Any())
                        {
                            var internalParticipantId = accountHandlerServices.GetInternalParticipant(appSettings.AFCInternalParticipantKey);

                            var notExistedAccounts = accountHandlerServices.GetAccountNotExist(accountInfo.Select(c => c.B2CCustId), internalParticipantId);

                            syncAFCServices.SyncAccounts(accountInfo, notExistedAccounts, internalParticipantId, FromFlow.ManualTrigger);
                        }
                    }
                }
            }
        }
    }

    #endregion Private Methods
}