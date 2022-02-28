namespace Fanex.NAP.SPU.AFC.Services.SyncAFCData
{
    using Fanex.Data.Repository;
    using Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData;
    using Newtonsoft.Json;
    using System.Collections.Generic;

    public interface ISyncAFCDataAccess
    {
        DBResult InsertAccounts(int? participantID, IEnumerable<AccountViewModel> accounts);

        DBResult UpdateAccounts(int? participantID, IEnumerable<AccountViewModel> accounts);

        DBResult DeleteAccounts(int? participantID, IEnumerable<string> b2cAccountIds);

        IEnumerable<GetB2CEntity> GetB2CCustIds(string custIds);

        IEnumerable<FraudCustomerEntity> ScanChangedCategoryToSyncAFC();
    }

    public class SyncAFCDataAccess : ISyncAFCDataAccess
    {
        private readonly IDynamicRepository repository;

        public SyncAFCDataAccess()
        {
            repository = new DynamicRepository();
        }

        public DBResult InsertAccounts(int? participantID, IEnumerable<AccountViewModel> accounts)
        {
            var criteria = new AccountInsertCriteria()
            {
                JsonAccountInfo = JsonConvert.SerializeObject(accounts),
                ParticipantID = participantID
            };

            repository.Execute(criteria);

            return new DBResult
            {
                StatusCode = string.IsNullOrEmpty(criteria.ErrorMessage) ? 1 : 0,
                ErrorMessage = criteria.ErrorMessage
            };
        }

        public DBResult UpdateAccounts(int? participantID, IEnumerable<AccountViewModel> accounts)
        {
            var criteria = new AccountUpdateCriteria()
            {
                JsonAccountInfo = JsonConvert.SerializeObject(accounts),
                ParticipantID = participantID
            };

            repository.Execute(criteria);

            return new DBResult
            {
                StatusCode = string.IsNullOrEmpty(criteria.ErrorMessage) ? 1 : 0,
                ErrorMessage = criteria.ErrorMessage
            };
        }

        public DBResult DeleteAccounts(int? participantID, IEnumerable<string> accounts)
        {
            var criteria = new AccountDeleteCriteria()
            {
                JsonAccountInfo = JsonConvert.SerializeObject(accounts),
                ParticipantID = participantID
            };

            repository.Execute(criteria);

            return new DBResult
            {
                StatusCode = 1,
                ErrorMessage = string.Empty
            };
        }

        public IEnumerable<GetB2CEntity> GetB2CCustIds(string custIds)
        {
            var criteria = new GetB2CustIdsCriteria
            {
                CustIds = custIds
            };

            return repository.Fetch<GetB2CEntity>(criteria);
        }

        public IEnumerable<FraudCustomerEntity> ScanChangedCategoryToSyncAFC()
        {
            var criteria = new ScanCCToSyncAFCCriteria();

            return repository.Fetch<FraudCustomerEntity>(criteria);
        }
    }
}