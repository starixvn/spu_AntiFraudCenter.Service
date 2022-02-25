namespace Fanex.NAP.SPU.AFC.Services.TriggerSyncAFC
{
    using Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData;
    using global::SPU.AntiFraudCenter.Nova88.Services.Cryptographic;
    using Sentry;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

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
            if (custIds != null && custIds.Any())
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

                if (accountInfo != null && accountInfo.Any())
                {
                    return EncryptAccount(accountInfo.ToList());
                }
            }

            return Enumerable.Empty<AccountViewModel>();
        }

        #region Private methods

        private IEnumerable<AccountViewModel> MappingFrauIDs(IEnumerable<AccountViewModel> accounts, IEnumerable<CustToSyncAFCEntity> fraudIDs)
        {
            if (accounts != null && accounts.Any() && fraudIDs != null && fraudIDs.Any())
            {
                var accInfo = accounts.Join(fraudIDs?.Distinct(),
                                   a => a.CustId,
                                   b => b.CustID,
                                   (a, b) => new { a, b })
                                   .Where(c => c.b.FraudIDList != null && c.b.FraudIDList.Any())
                                   .Select(c => new AccountViewModel
                                   {
                                       CustId = c.a.CustId,
                                       B2CCustId = c.a.B2CCustId,
                                       CountryCode = new RegionInfo(c.a.CountryCode)?.ThreeLetterISORegionName ?? string.Empty,
                                       Phone = c.a.Phone,
                                       BankName = c.a.BankName,
                                       BankAccountID = c.a.BankAccountID,
                                       Email = c.a.Email,
                                       FullName = c.a.FullName,
                                       FraudID = c.b?.FraudIDList
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