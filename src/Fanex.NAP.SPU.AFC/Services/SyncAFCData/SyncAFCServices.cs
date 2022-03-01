namespace Fanex.NAP.SPU.AFC.Services.SyncAFCData
{
    using Fanex.NAP.SPU.AFC.AccountHandlerServices;
    using Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData;
    using Fanex.NAP.SPU.AFC.Shares;
    using Newtonsoft.Json;
    using Sentry;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using static Fanex.NAP.SPU.AFC.Shares.Consts;

    public interface ISyncAFCServices
    {
        IEnumerable<FraudCustomerEntity> ScanChangedCategoryToSyncAFC();

        IEnumerable<InsertAccountModel> GetAccount(IEnumerable<FraudCustomerEntity> fraudCustIds, string url, FromFlow fromFlow);

        void SyncAccounts(IEnumerable<InsertAccountModel> accountInfo, IEnumerable<string> notExistedAccount, int? internalParticipantId, FromFlow fromFlow);

        DBResult DeleteAccounts(IEnumerable<string> accounts, int? participantID);

        IEnumerable<GetB2CEntity> GetB2CCustIds(IEnumerable<FraudCustomerEntity> fraudCustIds);

        IEnumerable<string> GetAccountToDelete(IEnumerable<string> b2cCustIds, int? participantIds);
    }

    public class SyncAFCServices : ISyncAFCServices
    {
        private readonly ISyncAFCDataAccess syncAFCDataAccess;
        private readonly IB2CAccountServices b2CAccountServices;
        private readonly IAccountHandlerServices accountHandlerServices;
        private readonly IErrorHandleServices errorHandleServices;
        private readonly ISentryClient sentryClient;
        private const string ServiceName = "AntiFraudCenter";

        public SyncAFCServices(ISyncAFCDataAccess syncAFCDataAccess,
            IB2CAccountServices b2CAccountServices,
            IAccountHandlerServices accountHandlerServices,
            IErrorHandleServices errorHandleServices,
            ISentryClient sentryClient)
        {
            this.syncAFCDataAccess = syncAFCDataAccess;
            this.b2CAccountServices = b2CAccountServices;
            this.accountHandlerServices = accountHandlerServices;
            this.errorHandleServices = errorHandleServices;
            this.sentryClient = sentryClient;
        }

        public IEnumerable<FraudCustomerEntity> ScanChangedCategoryToSyncAFC()
        {
            return syncAFCDataAccess.ScanChangedCategoryToSyncAFC();
        }

        public IEnumerable<InsertAccountModel> GetAccount(IEnumerable<FraudCustomerEntity> fraudCustIds, string url, FromFlow fromFlow)
        {
            if (fraudCustIds != null && fraudCustIds.Any())
            {
                var b2cCustIds = GetB2CCustIds(fraudCustIds);

                if (b2cCustIds != null && b2cCustIds.Any())
                {
                    var account = b2CAccountServices.GetAccountFromNova88(b2cCustIds.Select(c => c.B2CCustId), fraudCustIds.Select(c => c.CustID), url, fromFlow);

                    var accountInfo = accountHandlerServices.MappingFrauIDs(account.Result, fraudCustIds, b2cCustIds);

                    if (accountInfo != null && accountInfo.Any())
                    {
                        return accountHandlerServices.EncryptAccount(accountInfo);
                    }
                }
            }

            return Enumerable.Empty<InsertAccountModel>();
        }

        public IEnumerable<GetB2CEntity> GetB2CCustIds(IEnumerable<FraudCustomerEntity> fraudCustIds)
        {
            if (fraudCustIds != null && fraudCustIds.Any())
            {
                return syncAFCDataAccess.GetB2CCustIds(string.Join(",", fraudCustIds.Select(c => c.CustID)?.Distinct())) ?? Enumerable.Empty<GetB2CEntity>();
            }

            return Enumerable.Empty<GetB2CEntity>();
        }

        /// <summary>
        /// Update/Insert B2CAccount to AFC Database
        /// </summary>
        public void SyncAccounts(IEnumerable<InsertAccountModel> accountInfo, IEnumerable<string> notExistedAccount, int? internalParticipantId, FromFlow fromFlow)
        {
            if (accountInfo == null || !accountInfo.Any())
            {
                return;
            }

            var accToInsert = notExistedAccount.Any() ? accountInfo.Where(c => notExistedAccount.Contains(c.B2CCustId)) : null;
            var accToUpdate = notExistedAccount.Any() ? accountInfo.Where(c => !notExistedAccount.Contains(c.B2CCustId)) : accountInfo;

            Parallel.Invoke(() =>
            {
                try
                {
                    if (accToInsert != null && accToInsert.Any())
                    {
                        var result = InsertAccounts(accToInsert, internalParticipantId);

                        if (result.StatusCode == 0)
                        {
                            errorHandleServices.InsertErrorLog(internalParticipantId, $"{fromFlow} - {result.ErrorMessage}", JsonConvert.SerializeObject(accToInsert.Select(c => c.CustId)));
                            Utilities.LogSentryError(ServiceName, $"{fromFlow} - Insert Accounts fail", accToInsert.Select(c => c.CustId), new Exception(result.ErrorMessage), sentryClient, "CustIds");
                        }
                    }
                }
                catch (Exception ex)
                {
                    errorHandleServices.InsertErrorLog(internalParticipantId, $"{fromFlow} - {ex.Message}", JsonConvert.SerializeObject(accToInsert?.Select(c => c.CustId)));
                    Utilities.LogSentryError(ServiceName, $"{fromFlow} - Insert Accounts fail", accToInsert?.Select(c => c.CustId), ex, sentryClient, "CustIds");
                }
            },
            () =>
            {
                try
                {
                    if (accToUpdate != null && accToUpdate.Any())
                    {
                        var result = UpdateAccounts(accToUpdate, internalParticipantId);

                        if (result.StatusCode == 0)
                        {
                            errorHandleServices.InsertErrorLog(internalParticipantId, $"{fromFlow} - {result.ErrorMessage}", JsonConvert.SerializeObject(accToUpdate.Select(c => c.CustId)));
                            Utilities.LogSentryError(ServiceName, $"{fromFlow} - Update Accounts fail", accToUpdate.Select(c => c.CustId), new Exception(result.ErrorMessage), sentryClient, "CustIds");
                        }
                    }
                }
                catch (Exception ex)
                {
                    errorHandleServices.InsertErrorLog(internalParticipantId, $"{fromFlow} - {ex.Message}", JsonConvert.SerializeObject(accToUpdate?.Select(c => c.CustId)));
                    Utilities.LogSentryError(ServiceName, $"{fromFlow} - Update Accounts fail", accToUpdate?.Select(c => c.CustId), ex, sentryClient, "CustIds");
                }
            });
        }

        public DBResult DeleteAccounts(IEnumerable<string> accounts, int? participantID)
        {
            if (accounts != null && accounts.Any() && participantID != null)
            {
                return syncAFCDataAccess.DeleteAccounts(participantID, accounts);
            }

            return new DBResult() { StatusCode = 1, ErrorMessage = string.Empty };
        }

        public IEnumerable<string> GetAccountToDelete(IEnumerable<string> b2cCustIds, int? participantIds)
        {
            if (b2cCustIds != null && b2cCustIds.Any())
            {
                var notExistedAccounts = accountHandlerServices.GetAccountNotExist(b2cCustIds, participantIds);

                return b2cCustIds.Except(notExistedAccounts);
            }

            return Enumerable.Empty<string>();
        }

        #region Private methods

        private DBResult InsertAccounts(IEnumerable<InsertAccountModel> accounts, int? participantID)
        {
            if (accounts != null && accounts.Any() && participantID != null)
            {
                return syncAFCDataAccess.InsertAccounts(participantID, accounts);
            }

            return new DBResult() { StatusCode = 1, ErrorMessage = string.Empty };
        }

        private DBResult UpdateAccounts(IEnumerable<InsertAccountModel> accounts, int? participantID)
        {
            if (accounts != null && accounts.Any() && participantID != null)
            {
                return syncAFCDataAccess.UpdateAccounts(participantID, accounts);
            }

            return new DBResult() { StatusCode = 1, ErrorMessage = string.Empty };
        }

        #endregion Private methods
    }
}