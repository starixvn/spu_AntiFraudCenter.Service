namespace Fanex.NAP.SPU.AFC.AccountHandlerServices
{
    using Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData;
    using Fanex.NAP.SPU.AFC.Shares;
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

        IEnumerable<InsertAccountModel> ProcessAccountData(IEnumerable<AccountParameterModel> accounts, IEnumerable<FraudCustomerEntity> fraudIDs);

        IEnumerable<InsertAccountModel> MappingFrauIDs(IEnumerable<GetAccountModel> accounts, IEnumerable<FraudCustomerEntity> fraudIDs, IEnumerable<GetB2CEntity> b2cCustIDs);
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

        public IEnumerable<InsertAccountModel> ProcessAccountData(IEnumerable<AccountParameterModel> accounts, IEnumerable<FraudCustomerEntity> fraudIDs)
        {
            if (accounts != null && accounts.Any() && fraudIDs != null && fraudIDs.Any())
            {
                var accountInfo = MappingFrauIDs(accounts, fraudIDs);

                if (accountInfo != null && accountInfo.Any())
                {
                    return EncryptAccount(accountInfo.ToList());
                }
            }

            return Enumerable.Empty<InsertAccountModel>();
        }

        public IEnumerable<InsertAccountModel> MappingFrauIDs(IEnumerable<GetAccountModel> accounts, IEnumerable<FraudCustomerEntity> fraudIDs, IEnumerable<GetB2CEntity> b2cCustIDs)
        {
            if (IsValid(accounts, fraudIDs, b2cCustIDs))
            {
                var groupBankAccount = accounts.GroupBy(a => a.B2CCustId)
                                        .Select(c => new InsertAccountModel
                                        {
                                            B2CCustId = c.Key,
                                            CountryCode = c.Select(c=>c.CountryCode).FirstOrDefault(),
                                            DocumentID = c.Select(c => c.DocumentID).FirstOrDefault(),
                                            DocumentTypeID = c.Select(c => c.DocumentTypeID).FirstOrDefault(),
                                            Email = c.Select(c=>c.Email).FirstOrDefault(),
                                            FullName = c.Select(c => c.FullName).FirstOrDefault(),
                                            Phone = c.Select(c => c.Phone).FirstOrDefault(),
                                            BankAccount = c.Select(c => new BankAccountModel { BankName = c.BankName, BankAccountID = c.BankAccountID }) 
                                        });


                var accountResult = groupBankAccount.GroupJoin(b2cCustIDs,
                                           a => a.B2CCustId,
                                           b => b.B2CCustId,
                                           (a, b) => new { a, b })
                                       .SelectMany(
                                            group => group.b.DefaultIfEmpty(),
                                            (group, temp) => new InsertAccountModel
                                            {
                                                CustId = temp != null ? temp.CustId : 0,
                                                B2CCustId = group.a.B2CCustId,
                                                CountryCode = group.a.CountryCode,
                                                Phone = group.a.Phone,
                                                BankAccount = group.a.BankAccount,
                                                Email = group.a.Email,
                                                FullName = group.a.FullName
                                            })
                                        .GroupJoin(fraudIDs,
                                            a => a.CustId,
                                            f => f.CustID,
                                            (a, f) => new { a, f })
                                        .SelectMany(
                                           group => group.f.DefaultIfEmpty(),
                                           (group, temp) => new InsertAccountModel
                                           {
                                               CustId = group.a.CustId,
                                               B2CCustId = group.a.B2CCustId,
                                               CountryCode = string.IsNullOrEmpty(group.a.CountryCode) ? string.Empty : new RegionInfo(group.a.CountryCode)?.ThreeLetterISORegionName,
                                               Phone = group.a.Phone,
                                               BankAccount = group.a.BankAccount,
                                               Email = group.a.Email,
                                               FullName = group.a.FullName,
                                               FraudID = temp?.FraudIDList
                                           });

                return accountResult ?? Enumerable.Empty<InsertAccountModel>();
            }

            return Enumerable.Empty<InsertAccountModel>();
        }

        #region Private methods

        private IEnumerable<InsertAccountModel> EncryptAccount(IEnumerable<InsertAccountModel> data)
        {
            if (data != null && data.Any())
            {
                var result = new List<InsertAccountModel>();

                data.ToList().ForEach(c =>
                {
                    result.Add(new InsertAccountModel
                    {
                        B2CCustId = c.B2CCustId,
                        FullName = c.FullName,
                        CountryCode = c.CountryCode,
                        Email = string.IsNullOrEmpty(c.Email) ? null : CryptographicHelper.AESEncrypt(c.Email, CryptographicConfig.EncryptedKey, CryptographicConfig.IVKey),
                        Phone = string.IsNullOrEmpty(c.Phone) ? null : CryptographicHelper.AESEncrypt(c.Phone, CryptographicConfig.EncryptedKey, CryptographicConfig.IVKey),
                        DocumentID = string.IsNullOrEmpty(c.DocumentID) ? Consts.Unknown : CryptographicHelper.AESEncrypt(c.DocumentID, CryptographicConfig.EncryptedKey, CryptographicConfig.IVKey),
                        DocumentTypeID = c.DocumentTypeID,
                        BankAccount = EncryptBankAccount(c.BankAccount?.ToList()),
                        FraudID = c.FraudID
                    });
                });

                return result;
            }

            return Enumerable.Empty<InsertAccountModel>();
        }

        private IEnumerable<InsertAccountModel> MappingFrauIDs(IEnumerable<AccountParameterModel> accounts, IEnumerable<FraudCustomerEntity> fraudIDs)
        {
            if (accounts != null && accounts.Any() && fraudIDs != null && fraudIDs.Any())
            {
                var groupBankAccount = accounts.GroupBy(a => a.B2CCustId)
                                        .Select(c => new InsertAccountModel
                                        {
                                            B2CCustId = c.Key,
                                            CustId = c.Select(c => c.CustId).FirstOrDefault(),
                                            CountryCode = c.Select(c => c.CountryCode).FirstOrDefault(),
                                            DocumentID = c.Select(c => c.DocumentID).FirstOrDefault(),
                                            DocumentTypeID = c.Select(c => c.DocumentTypeID).FirstOrDefault(),
                                            Email = c.Select(c => c.Email).FirstOrDefault(),
                                            FullName = c.Select(c => c.FullName).FirstOrDefault(),
                                            Phone = c.Select(c => c.Phone).FirstOrDefault(),
                                            BankAccount = c.Select(c => new BankAccountModel { BankName = c.BankName, BankAccountID = c.BankAccountID })
                                        });

                var accInfo = groupBankAccount.Join(fraudIDs?.Distinct(),
                                   a => a.CustId,
                                   b => b.CustID,
                                   (a, b) => new { a, b })
                                   .Where(c => c.b.FraudIDList != null && c.b.FraudIDList.Any())
                                   .Select(c => new InsertAccountModel
                                   {
                                       CustId = c.a.CustId,
                                       B2CCustId = c.a.B2CCustId,
                                       CountryCode = new RegionInfo(c.a.CountryCode)?.ThreeLetterISORegionName ?? string.Empty,
                                       Phone = c.a.Phone,
                                       BankAccount = c.a.BankAccount,
                                       Email = c.a.Email,
                                       FullName = c.a.FullName,
                                       FraudID = c.b?.FraudIDList
                                   });

                return accInfo ?? Enumerable.Empty<InsertAccountModel>();
            }

            return Enumerable.Empty<InsertAccountModel>();
        }

        private static bool IsValid(IEnumerable<GetAccountModel> accountInfo, IEnumerable<FraudCustomerEntity> fraudIDs, IEnumerable<GetB2CEntity> b2cCustIDs)
        {
            if (accountInfo != null && fraudIDs != null && b2cCustIDs != null)
            {
                return accountInfo.Any() && fraudIDs.Any() && b2cCustIDs.Any();
            }

            return false;
        }

        private static IEnumerable<BankAccountModel> EncryptBankAccount(IEnumerable<BankAccountModel> bankAccount)
        {
            if (bankAccount != null && bankAccount.Any())
            {
                bankAccount.ToList().ForEach(c =>
                {
                    c.BankAccountID = string.IsNullOrEmpty(c.BankAccountID) ? Consts.Unknown : CryptographicHelper.AESEncrypt(c.BankAccountID, CryptographicConfig.EncryptedKey, CryptographicConfig.IVKey);
                    c.BankName = string.IsNullOrEmpty(c.BankName) ? Consts.Unknown : c.BankName;
                });

                return bankAccount;
            }

            return Enumerable.Empty<BankAccountModel>();
        }

        #endregion Private methods
    }
}