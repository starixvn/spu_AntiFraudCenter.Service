namespace Fanex.NAP.SPU.AFC.Services.SyncAFCData
{
    using Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData;
    using Fanex.NAP.SPU.AFC.Shares;
    using Newtonsoft.Json;
    using Sentry;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;

    public interface ISyncAFCServices
    {
        IEnumerable<CustToSyncAFCEntity> GetCustChangedCategory();

        IEnumerable<AccountViewModel> GetAccount(IEnumerable<CustToSyncAFCEntity> fraudCustIds, string url);

        IEnumerable<string> GetAccountNotExist(IEnumerable<string> accInfo, int? internalParticipantId);

        void SyncAccounts(IEnumerable<AccountViewModel> accountInfo, IEnumerable<string> notExistedAccount, int? internalParticipantId);

        DBResult DeleteAccounts(IEnumerable<string> accounts, int? participantID);

        int? GetInternalParticipant(string internalKey);

        IEnumerable<GetB2CEntity> GetB2CAccountId(IEnumerable<CustToSyncAFCEntity> fraudCustIds);
    }

    public class SyncAFCServices : ISyncAFCServices
    {
        private readonly ISyncAFCDataAccess syncAFCDataAccess;
        private readonly IB2CAccountServices b2CAccountServices;
        private readonly IErrorHandleServices errorHandleServices;
        private readonly ISentryClient sentryClient;
        private const string ServiceName = "Service AntiFraudCenter";

        public SyncAFCServices(ISyncAFCDataAccess syncAFCDataAccess,
            IB2CAccountServices b2CAccountServices,
            IErrorHandleServices errorHandleServices,
            ISentryClient sentryClient)
        {
            this.syncAFCDataAccess = syncAFCDataAccess;
            this.b2CAccountServices = b2CAccountServices;
            this.errorHandleServices = errorHandleServices;
            this.sentryClient = sentryClient;
        }

        public IEnumerable<CustToSyncAFCEntity> GetCustChangedCategory()
        {
            return syncAFCDataAccess.GetCustChangedCategory();
        }

        public IEnumerable<AccountViewModel> GetAccount(IEnumerable<CustToSyncAFCEntity> fraudCustIds, string url)
        {
            if (fraudCustIds != null && fraudCustIds.Any())
            {
                var b2cCustIds = GetB2CAccountId(fraudCustIds);

                if (b2cCustIds != null && b2cCustIds.Any())
                {
                    var account = b2CAccountServices.GetB2CAccountFromNova88(b2cCustIds.Select(c => c.B2CCustId), url);

                    return MappingFrauIDs(account.Result, fraudCustIds, b2cCustIds);
                }
            }

            return Enumerable.Empty<AccountViewModel>();
        }

        public IEnumerable<string> GetAccountNotExist(IEnumerable<string> accInfo, int? internalParticipantId)
        {
            if (accInfo != null && accInfo.Any() && internalParticipantId != null)
            {
                var result = syncAFCDataAccess.CheckAccountValidation(internalParticipantId, accInfo.Select(c => new CheckAccountExistEntity { AccountID = c }));

                if (result != null && result.Any())
                {
                    var notExistedAcc = result.Where(c => string.Equals(c.Status, "Not Exist")).Select(c => c.AccountID);

                    return notExistedAcc ?? Enumerable.Empty<string>();
                }
            }

            return Enumerable.Empty<string>();
        }

        public int? GetInternalParticipant(string internalKey)
        {
            var participants = syncAFCDataAccess.GetParticipant();

            if (participants != null && participants.Any())
            {
                var internalInfo = participants.SingleOrDefault(c => string.Equals(c.SecretKey, internalKey));

                if (internalInfo == null)
                {
                    throw new Exception("AFC - Internal Key is Invalid");
                }

                return internalInfo.ParticipantID;
            }

            return null;
        }

        public IEnumerable<GetB2CEntity> GetB2CAccountId(IEnumerable<CustToSyncAFCEntity> fraudCustIds)
        {
            if (fraudCustIds != null && fraudCustIds.Any())
            {
                return syncAFCDataAccess.GetB2AccountIds(string.Join(",", fraudCustIds.Select(c => c.CustID)?.Distinct())) ?? Enumerable.Empty<GetB2CEntity>();
            }

            return Enumerable.Empty<GetB2CEntity>();
        }

        /// <summary>
        /// Update/Insert B2CAccount to AFC Database
        /// </summary>
        public void SyncAccounts(IEnumerable<AccountViewModel> accountInfo, IEnumerable<string> notExistedAccount, int? internalParticipantId)
        {
            if (accountInfo == null || !accountInfo.Any())
            {
                return;
            }

            var accToInsert = notExistedAccount.Any() ? accountInfo.Where(c => notExistedAccount.Contains(c.AccountID)) : null;
            var accToUpdate = notExistedAccount.Any() ? accountInfo.Where(c => !notExistedAccount.Contains(c.AccountID)) : accountInfo;

            Parallel.Invoke(() =>
            {
                try
                {
                    var result = InsertAccounts(accToInsert, internalParticipantId);

                    if (result.StatusCode == 0)
                    {
                        errorHandleServices.InsertErrorLog(internalParticipantId, result.ErrorMessage, JsonConvert.SerializeObject(accToInsert));
                        Utilities.LogSentryError(ServiceName, $"AFC Service - Insert Accounts fail", accToInsert, new Exception(result.ErrorMessage), sentryClient);
                    }
                }
                catch (Exception ex)
                {
                    Utilities.LogSentryError(ServiceName, "AFC Service - Insert Accounts fail", accToInsert, ex, sentryClient);
                    errorHandleServices.InsertErrorLog(internalParticipantId, ex.Message, JsonConvert.SerializeObject(accToInsert));
                }
            },
            () =>
            {
                try
                {
                    var result = UpdateAccounts(accToUpdate, internalParticipantId);

                    if (result.StatusCode == 0)
                    {
                        errorHandleServices.InsertErrorLog(internalParticipantId, result.ErrorMessage, JsonConvert.SerializeObject(accToUpdate));
                        Utilities.LogSentryError(ServiceName, $"AFC Service - Update Accounts fail", accToUpdate, new Exception(result.ErrorMessage), sentryClient);
                    }
                }
                catch (Exception ex)
                {
                    Utilities.LogSentryError(ServiceName, "AFC Service - Update Accounts fail", accToUpdate, ex, sentryClient);
                    errorHandleServices.InsertErrorLog(internalParticipantId, ex.Message, JsonConvert.SerializeObject(accToUpdate));
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

        #region Private methods

        private IEnumerable<AccountViewModel> MappingFrauIDs(IEnumerable<GetAccountModel> accounts, IEnumerable<CustToSyncAFCEntity> fraudIDs, IEnumerable<GetB2CEntity> b2cCustIDs)
        {
            if (IsValid(accounts, fraudIDs, b2cCustIDs))
            {
                var accInfo = accounts.GroupJoin(b2cCustIDs,
                                   a => a.AccountID,
                                   b => b.B2CCustId,
                                   (a, b) => new { a, b })
                               .SelectMany(
                                    group => group.b.DefaultIfEmpty(),
                                    (group, temp) => new AccountViewModel
                                    {
                                        CustId = temp.CustId,
                                        AccountID = group.a.AccountID,
                                        CountryCode = group.a.CountryCode,
                                        Phone = group.a.Phone,
                                        BankName = group.a.BankName,
                                        BankAccountID = group.a.BankAccountID,
                                        Email = group.a.Email,
                                        FullName = group.a.FullName
                                    })
                                .GroupJoin(fraudIDs,
                                    a => a.CustId,
                                    f => f.CustID,
                                    (a, f) => new { a, f })
                                .SelectMany(
                                   group => group.f.DefaultIfEmpty(),
                                   (group, temp) => new AccountViewModel
                                   {
                                       CustId = group.a.CustId,
                                       AccountID = group.a.AccountID,
                                       CountryCode = new RegionInfo(group.a.CountryCode)?.ThreeLetterISORegionName ?? string.Empty,
                                       Phone = group.a.Phone,
                                       BankName = group.a.BankName,
                                       BankAccountID = group.a.BankAccountID,
                                       Email = group.a.Email,
                                       FullName = group.a.FullName,
                                       FraudID = temp?.FraudIDList
                                   });

                return accInfo ?? Enumerable.Empty<AccountViewModel>();
            }

            return Enumerable.Empty<AccountViewModel>();
        }

        private DBResult InsertAccounts(IEnumerable<AccountViewModel> accounts, int? participantID)
        {
            if (accounts != null && accounts.Any() && participantID != null)
            {
                return syncAFCDataAccess.InsertAccounts(participantID, accounts);
            }

            return new DBResult() { StatusCode = 1, ErrorMessage = string.Empty };
        }

        private DBResult UpdateAccounts(IEnumerable<AccountViewModel> accounts, int? participantID)
        {
            if (accounts != null && accounts.Any() && participantID != null)
            {
                return syncAFCDataAccess.UpdateAccounts(participantID, accounts);
            }

            return new DBResult() { StatusCode = 1, ErrorMessage = string.Empty };
        }

        private static bool IsValid(IEnumerable<GetAccountModel> accountInfo, IEnumerable<CustToSyncAFCEntity> fraudIDs, IEnumerable<GetB2CEntity> b2cCustIDs)
        {
            if (accountInfo != null && fraudIDs != null && b2cCustIDs != null)
            {
                return accountInfo.Any() && fraudIDs.Any() && b2cCustIDs.Any();
            }

            return false;
        }

        private static bool IsLogError(string errorMessage)
        {
            if (!string.IsNullOrEmpty(errorMessage))
            {
                return !errorMessage.Contains("Not Exist");
            }

            return true;
        }

        #endregion Private methods
    }
}