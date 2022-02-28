namespace Fanex.NAP.SPU.AFC.AccountHandlerServices
{
    using Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData;
    using global::SPU.AntiFraudCenter.Nova88.Services.Cryptographic;
    using Sentry;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    public interface IAccountHandlerServices
    {
        IEnumerable<FraudCustomerEntity> GetFraudCases(IEnumerable<Int64> custIds);

        int? GetInternalParticipant(string internalKey);

        IEnumerable<string> GetAccountNotExist(IEnumerable<string> accInfo, int? internalParticipantId);

        IEnumerable<AccountViewModel> EncryptAccount(IEnumerable<AccountViewModel> accounts, IEnumerable<FraudCustomerEntity> fraudIDs);

        IEnumerable<AccountViewModel> EncryptAccount(IEnumerable<AccountViewModel> data);

        IEnumerable<AccountViewModel> MappingFrauIDs(IEnumerable<GetAccountModel> accounts, IEnumerable<FraudCustomerEntity> fraudIDs, IEnumerable<GetB2CEntity> b2cCustIDs);
    }

    public class AccountHandlerServices : IAccountHandlerServices
    {
        private readonly IAccountHandlerDataAccess accountHandlerDataAccess;

        public AccountHandlerServices(IAccountHandlerDataAccess triggerSyncAFCDataAccess, ISentryClient sentryClient)
        {
            this.accountHandlerDataAccess = triggerSyncAFCDataAccess;
        }

        public IEnumerable<FraudCustomerEntity> GetFraudCases(IEnumerable<Int64> custIds)
        {
            if (custIds != null && custIds.Any())
            {
                var custToString = string.Join(",", custIds);

                return accountHandlerDataAccess.GetFraudCases(custToString);
            }

            return Enumerable.Empty<FraudCustomerEntity>();
        }

        public int? GetInternalParticipant(string internalKey)
        {
            var participants = accountHandlerDataAccess.GetParticipant();

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

        public IEnumerable<string> GetAccountNotExist(IEnumerable<string> accInfo, int? internalParticipantId)
        {
            if (accInfo != null && accInfo.Any() && internalParticipantId != null)
            {
                var result = accountHandlerDataAccess.CheckAccountExistence(internalParticipantId, accInfo.Select(c => new AccountExistenceEntity { AccountID = c }));

                if (result != null && result.Any())
                {
                    var notExistedAcc = result.Where(c => string.Equals(c.Status, "Not Exist")).Select(c => c.AccountID);

                    return notExistedAcc ?? Enumerable.Empty<string>();
                }
            }

            return Enumerable.Empty<string>();
        }

        public IEnumerable<AccountViewModel> EncryptAccount(IEnumerable<AccountViewModel> accounts, IEnumerable<FraudCustomerEntity> fraudIDs)
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

        public IEnumerable<AccountViewModel> EncryptAccount(IEnumerable<AccountViewModel> data)
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

        public IEnumerable<AccountViewModel> MappingFrauIDs(IEnumerable<GetAccountModel> accounts, IEnumerable<FraudCustomerEntity> fraudIDs, IEnumerable<GetB2CEntity> b2cCustIDs)
        {
            if (IsValid(accounts, fraudIDs, b2cCustIDs))
            {
                var accInfo = accounts.GroupJoin(b2cCustIDs,
                                   a => a.B2CCustId,
                                   b => b.B2CCustId,
                                   (a, b) => new { a, b })
                               .SelectMany(
                                    group => group.b.DefaultIfEmpty(),
                                    (group, temp) => new AccountViewModel
                                    {
                                        CustId = temp.CustId,
                                        B2CCustId = group.a.B2CCustId,
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
                                       B2CCustId = group.a.B2CCustId,
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

        #region Private methods

        public IEnumerable<AccountViewModel> MappingFrauIDs(IEnumerable<AccountViewModel> accounts, IEnumerable<FraudCustomerEntity> fraudIDs)
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

        private static bool IsValid(IEnumerable<GetAccountModel> accountInfo, IEnumerable<FraudCustomerEntity> fraudIDs, IEnumerable<GetB2CEntity> b2cCustIDs)
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