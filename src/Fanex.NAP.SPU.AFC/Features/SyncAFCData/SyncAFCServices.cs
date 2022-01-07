namespace Fanex.NAP.SPU.AFC.Features.SyncAFCData
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
    using static Fanex.NAP.SPU.AFC.Shares.Consts;

    public interface ISyncAFCServices
    {
        IEnumerable<CustToSyncAFCEntity> GetCustomerToSyncAFC();

        void InsertAFCAccountsAsync(IEnumerable<CustToSyncAFCEntity> custToCheck, string insertURL, string updateURL, string internalKey);

        void DeleteAFCAccountsAsync(IEnumerable<int> custIds, string url, string internalKey);
    }

    public class SyncAFCServices : ISyncAFCServices
    {
        private readonly ISyncAFCDataAccess syncAFCDataAccess;
        private readonly ISentryClient sentryClient;
        private const string ServiceName = "Service AntiFraudCenter";

        public SyncAFCServices(ISyncAFCDataAccess syncAFCDataAccess, ISentryClient sentryClient)
        {
            this.syncAFCDataAccess = syncAFCDataAccess;
            this.sentryClient = sentryClient;
        }

        public IEnumerable<CustToSyncAFCEntity> GetCustomerToSyncAFC()
        {
            return syncAFCDataAccess.GetCustomerToSyncAFC();
        }

        public void InsertAFCAccountsAsync(IEnumerable<CustToSyncAFCEntity> custToCheck, string insertURL, string updateURL, string internalKey)
        {
            try
            {
                if (custToCheck != null && custToCheck.Any())
                {
                    var accountInfo = GetAccountInfo(custToCheck);

                    if (accountInfo != null && accountInfo.Any())
                    {
                        var notExistedAccount = CheckAccountExist(accountInfo, internalKey);

                        SyncAccountToAFCDBAsync(accountInfo, notExistedAccount, insertURL, updateURL, internalKey);
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.LogSentryError(ServiceName, "Insert/Updte Accounts fail", custToCheck, ex, sentryClient);
            }
        }

        public async void DeleteAFCAccountsAsync(IEnumerable<int> custIds, string url, string internalKey)
        {
            try
            {
                if (custIds != null && custIds.Any())
                {
                    var b2cCustIds = syncAFCDataAccess.GetB2CustIds(string.Join(",", custIds));

                    if (b2cCustIds != null && b2cCustIds.Any())
                    {
                        var para = new DeleteAccountParasViewEntity
                        {
                            Key = internalKey,
                            Data = b2cCustIds
                        };

                        var result = await ApiHelper.Post<AccountDBResult>(url, para);

                        if (result.StatusCode != (int)HttpStatus.OK)
                        {
                            Utilities.LogSentryError(ServiceName, $"CTS.CC Service - Delete Accounts fail", custIds, new Exception(result.ErrorMessage), sentryClient);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.LogSentryError(ServiceName, "Delete Accounts fail", custIds, ex, sentryClient);
            }
        }

        #region Private methods

        private IEnumerable<GetAccountViewEntity> GetAccountInfo(IEnumerable<CustToSyncAFCEntity> fraudCustIds)
        {
            if (fraudCustIds != null && fraudCustIds.Any())
            {
                var b2cCustIds = syncAFCDataAccess.GetB2CustIds(string.Join(",", fraudCustIds.Select(c => c.CustID).Distinct()));

                if (b2cCustIds != null && b2cCustIds.Any())
                {
                    var b2cCustJson = JsonConvert.SerializeObject(b2cCustIds.Select(c => c.B2CCustId));

                    var accountInfo = syncAFCDataAccess.GetAccountInfo(b2cCustJson);

                    return MappingFrauIDs(accountInfo, fraudCustIds, b2cCustIds);
                }
            }

            return new List<GetAccountViewEntity>();
        }

        private static IEnumerable<GetAccountViewEntity> MappingFrauIDs(IEnumerable<GetAccountEntity> accountInfo, IEnumerable<CustToSyncAFCEntity> fraudIDs, IEnumerable<GetB2CEntity> b2cCustIDs)
        {
            if (IsValid(accountInfo, fraudIDs, b2cCustIDs))
            {
                var accInfo = accountInfo.GroupJoin(b2cCustIDs,
                                   a => a.CustId,
                                   b => b.B2CCustId,
                                   (a, b) => new { a, b })
                               .SelectMany(
                                    group => group.b.DefaultIfEmpty(),
                                    (group, temp) => new GetAccountViewEntity
                                    {
                                        CustId = temp?.CustId,
                                        B2CustId = group.a.CustId,
                                        CountryCode = group.a.CountryCode,
                                        PhoneNo = group.a.PhoneNo,
                                        AccountName = group.a.AccountName,
                                        AccountNum = group.a.AccountNum,
                                        Email = group.a.Email
                                    })
                                .GroupJoin(fraudIDs,
                                    a => a.CustId,
                                    f => f.CustID,
                                    (a, f) => new { a, f })
                                .SelectMany(
                                   group => group.f.DefaultIfEmpty(),
                                   (group, temp) => new GetAccountViewEntity
                                   {
                                       CustId = group.a.CustId,
                                       B2CustId = group.a.B2CustId,
                                       CountryCode = new RegionInfo(group.a.CountryCode)?.ThreeLetterISORegionName ?? string.Empty,
                                       PhoneNo = group.a.PhoneNo,
                                       AccountName = group.a.AccountName,
                                       AccountNum = group.a.AccountNum,
                                       Email = group.a.Email,
                                       FraudID = temp?.FraudIDList
                                   });

                return accInfo ?? new List<GetAccountViewEntity>();
            }

            return new List<GetAccountViewEntity>();
        }

        private IEnumerable<string> CheckAccountExist(IEnumerable<GetAccountViewEntity> accInfo, string internalKey)
        {
            if (accInfo != null && accInfo.Any())
            {
                var participants = syncAFCDataAccess.GetParticipant();

                if (participants != null && participants.Any())
                {
                    var internalInfo = participants.SingleOrDefault(c => string.Equals(c.SecretKey, internalKey));

                    if (internalInfo == null)
                    {
                        throw new Exception("AFC - Internal Key is Invalid");
                    }

                    var result = syncAFCDataAccess.CheckAccountValidation(internalInfo.ParticipantID, accInfo.Select(c => new CheckAccountExistEntity { AccountID = c.B2CustId }));

                    if (result != null && result.Any())
                    {
                        var notExistedAcc = result.Where(c => string.Equals(c.Status, "Not Exist")).Select(c => c.AccountID);

                        return notExistedAcc ?? new List<string>();
                    }
                }
            }

            return new List<string>();
        }

        /// <summary>
        /// Insert or Update AFC account
        /// </summary>
        /// <param name="accInfo"></param>
        /// <param name="isInsertAction"></param>
        private async Task<AccountDBResult> SyncAFCData(IEnumerable<GetAccountViewEntity> accInfo, string url, string internalKey)
        {
            if (accInfo != null && accInfo.Any())
            {
                var para = new InsertAccountParasViewEntity
                {
                    Key = internalKey,
                    Data = accInfo
                };

                return await ApiHelper.Post<AccountDBResult>(url, para);
            }

            return new AccountDBResult { StatusCode = (int)HttpStatus.OK, ErrorMessage = string.Empty };
        }

        private void SyncAccountToAFCDBAsync(IEnumerable<GetAccountViewEntity> accountInfo, IEnumerable<string> notExistedAccount, string insertURL, string updateURL, string internalKey)
        {
            if (accountInfo == null || !accountInfo.Any())
            {
                return;
            }

            var accToInsert = notExistedAccount.Any() ? accountInfo.Where(c => notExistedAccount.Contains(c.B2CustId)) : null;
            var accToUpdate = notExistedAccount.Any() ? accountInfo.Where(c => !notExistedAccount.Contains(c.B2CustId)) : accountInfo;

            Parallel.Invoke(async () =>
            {
                try
                {
                    var result = await SyncAFCData(accToInsert, insertURL, internalKey);

                    if (result.StatusCode != (int)HttpStatus.OK)
                    {
                        Utilities.LogSentryError(ServiceName, $"AFC Service - Insert Accounts fail", accToInsert, new Exception(result.ErrorMessage), sentryClient);
                    }
                }
                catch (Exception ex)
                {
                    Utilities.LogSentryError(ServiceName, "AFC Service - Insert Accounts fail", accToInsert, ex, sentryClient);
                }
            },
            async () =>
            {
                try
                {
                    var result = await SyncAFCData(accToUpdate, updateURL, internalKey);

                    if (result.StatusCode != (int)HttpStatus.OK)
                    {
                        Utilities.LogSentryError(ServiceName, $"AFC Service - Update Accounts fail", accToUpdate, new Exception(result.ErrorMessage), sentryClient);
                    }
                }
                catch (Exception ex)
                {
                    Utilities.LogSentryError(ServiceName, "AFC Service - Update Accounts fail", accToUpdate, ex, sentryClient);
                }
            });
        }

        private static bool IsValid(IEnumerable<GetAccountEntity> accountInfo, IEnumerable<CustToSyncAFCEntity> fraudIDs, IEnumerable<GetB2CEntity> b2cCustIDs)
        {
            if (accountInfo != null && fraudIDs != null && b2cCustIDs != null)
            {
                return accountInfo.Any() && fraudIDs.Any() && b2cCustIDs.Any();
            }

            return false;
        }

        #endregion Private methods
    }
}