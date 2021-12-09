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

    public interface ISyncAFCServices
    {
        IEnumerable<CustToSyncAFCEntity> GetCustomerToSyncAFC();

        void InsertAFCAccountsAsync(IEnumerable<CustToSyncAFCEntity> custToCheck, string insertURL, string updateURL);

        void DeleteAFCAccountsAsync(IEnumerable<int> custIds, string url);
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

        public void InsertAFCAccountsAsync(IEnumerable<CustToSyncAFCEntity> custToCheck, string insertURL, string updateURL)
        {
            try
            {
                if (custToCheck != null && custToCheck.Any())
                {
                    var accountInfo = GetAccountInfo(custToCheck);

                    if (accountInfo != null && accountInfo.Any())
                    {
                        var notExistedAccount = CheckAccountExist(accountInfo);

                        SyncAccountToAFCDBAsync(accountInfo, notExistedAccount, insertURL, updateURL);
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.LogSentryError(ServiceName, "Insert/Updte Accounts fail", custToCheck, ex, sentryClient);
            }
        }

        public void DeleteAFCAccountsAsync(IEnumerable<int> custIds, string url)
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
                            Key = Consts.AFCInternalKey,
                            Data = b2cCustIds
                        };

                        Task.WaitAll(ApiHelper.Post<bool>(url, para));
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

        private IEnumerable<GetAccountViewEntity> MappingFrauIDs(IEnumerable<GetAccountEntity> accountInfo
                                                      , IEnumerable<CustToSyncAFCEntity> fraudIDs
                                                      , IEnumerable<GetB2CEntity> b2cCustIDs)
        {
            if (accountInfo != null && fraudIDs != null && b2cCustIDs != null &&
                accountInfo.Any() && fraudIDs.Any() && b2cCustIDs.Any())
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

        private IEnumerable<string> CheckAccountExist(IEnumerable<GetAccountViewEntity> accInfo)
        {
            if (accInfo != null && accInfo.Any())
            {
                var participants = syncAFCDataAccess.GetParticipant();

                if (participants != null && participants.Any())
                {
                    var internalInfo = participants.SingleOrDefault(c => string.Equals(c.SecretKey, Consts.AFCInternalKey));

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
        private async Task SyncAFCData(IEnumerable<GetAccountViewEntity> accInfo, string url)
        {
            if (accInfo != null && accInfo.Any())
            {
                var para = new InsertAccountParasViewEntity
                {
                    Key = Consts.AFCInternalKey,
                    Data = accInfo
                };

                await ApiHelper.Post<bool>(url, para);
            }
        }

        private void SyncAccountToAFCDBAsync(IEnumerable<GetAccountViewEntity> accountInfo, IEnumerable<string> notExistedAccount, string insertURL, string updateURL)
        {
            if (accountInfo != null && accountInfo.Any())
            {
                var accToInsert = notExistedAccount.Any() ? accountInfo.Where(c => notExistedAccount.Contains(c.B2CustId)) : null;
                var accToUpdate = notExistedAccount.Any() ? accountInfo.Where(c => !notExistedAccount.Contains(c.B2CustId)) : accountInfo;
                
                var tasks = new List<Task>
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            await SyncAFCData(accToInsert, insertURL);
                        }
                        catch (Exception ex)
                        {
                           Utilities.LogSentryError(ServiceName, "CTS.CC Service - Insert Accounts fail", accToInsert, ex, sentryClient);
                        }
                    }),

                    Task.Run(async () =>
                    {
                        try
                        {
                            await SyncAFCData(accToUpdate, updateURL);
                        }
                        catch (Exception ex)
                        {
                           Utilities.LogSentryError(ServiceName, "Update Accounts fail", accToUpdate, ex, sentryClient);
                        }
                    })
                };

                Task.WaitAll(tasks.ToArray());
            }
        }

        #endregion Private methods
    }
}