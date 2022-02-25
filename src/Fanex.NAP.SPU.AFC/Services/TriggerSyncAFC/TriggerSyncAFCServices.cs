namespace Fanex.NAP.SPU.AFC.Services.TriggerSyncAFC
{
    using Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData;
    using Fanex.NAP.SPU.AFC.Shares;
    using global::SPU.AntiFraudCenter.Nova88.Services.Cryptographic;
    using Newtonsoft.Json;
    using Sentry;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;

    public interface ITriggerSyncAFCServices
    {
        IEnumerable<CustToSyncAFCEntity> GetFraudCases(IEnumerable<Int64> custIds);

        IEnumerable<AccountViewModel> GetAccountInfo(IEnumerable<AccountViewModel> accounts, IEnumerable<CustToSyncAFCEntity> fraudIDs);
    }

    public class TriggerSyncAFCServices : ITriggerSyncAFCServices
    {

        private readonly ITriggerSyncAFCDataAccess triggerSyncAFCDataAccess;
        private readonly ISentryClient sentryClient;
        private const string ServiceName = "Service AntiFraudCenter";

        public TriggerSyncAFCServices(ITriggerSyncAFCDataAccess triggerSyncAFCDataAccess, ISentryClient sentryClient)
        {
            this.triggerSyncAFCDataAccess = triggerSyncAFCDataAccess;
            this.sentryClient = sentryClient;
        }

        public IEnumerable<CustToSyncAFCEntity> GetFraudCases(IEnumerable<Int64> custIds)
        {
            if(custIds!=null && custIds.Any())
            {
                var custToString = string.Join(",", custIds);
                return triggerSyncAFCDataAccess.GetFraudCases(custToString);
            }

            return Enumerable.Empty<CustToSyncAFCEntity>();
        }

        public IEnumerable<AccountViewModel> GetAccountInfo(IEnumerable<AccountViewModel> accounts, IEnumerable<CustToSyncAFCEntity> fraudIDs)
        {
            if (accounts != null && accounts.Any() && fraudIDs != null && fraudIDs.Any())
            {
                var accountInfo = MappingFrauIDs(accounts, fraudIDs);

                if(accountInfo != null && accountInfo.Any())
                {
                    return EncryptAccount(accountInfo);
                }
            }

            return Enumerable.Empty<AccountViewModel>();


        }

    #region Private methods

    private IEnumerable<AccountViewModel> MappingFrauIDs(IEnumerable<AccountViewModel> accounts, IEnumerable<CustToSyncAFCEntity> fraudIDs)
    {
        if (accounts != null && accounts.Any() && fraudIDs != null && fraudIDs.Any())
        {
            var accInfo = accounts.GroupJoin(fraudIDs?.Distinct(),
                               a => a.CustId,
                               b => b.CustID,
                               (a, b) => new { a, b })
                           .SelectMany(
                                group => group.b.DefaultIfEmpty(),
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
                                    FraudID = temp?.FraudIDList ?? Enumerable.Empty<int>()
                                });


            return accInfo ?? Enumerable.Empty<AccountViewModel>();
        }

        return Enumerable.Empty<AccountViewModel>();
    }

    private IEnumerable<AccountViewModel> EncryptAccount(IEnumerable<AccountViewModel> data)
    {
        if (data != null && data.Any())
        {
            data.ToList().ForEach(c =>
            {
                c.Phone = string.IsNullOrEmpty(c.Phone) ? null : CryptographicHelper.AESEncrypt(c.Phone, CryptographicConfig.EncryptedKey, CryptographicConfig.IVKey);
                c.Email = string.IsNullOrEmpty(c.Email) ? null : CryptographicHelper.AESEncrypt(c.Email, CryptographicConfig.EncryptedKey, CryptographicConfig.IVKey);
                c.DocumentID = string.IsNullOrEmpty(c.DocumentID) ? null : CryptographicHelper.AESEncrypt(c.DocumentID, CryptographicConfig.EncryptedKey, CryptographicConfig.IVKey);
                c.BankAccountID = string.IsNullOrEmpty(c.BankAccountID) ? null : CryptographicHelper.AESEncrypt(c.BankAccountID, CryptographicConfig.EncryptedKey, CryptographicConfig.IVKey);
            });

            return data;
        }

        return new List<AccountViewModel>();
    }
    #endregion Private methods
}
}