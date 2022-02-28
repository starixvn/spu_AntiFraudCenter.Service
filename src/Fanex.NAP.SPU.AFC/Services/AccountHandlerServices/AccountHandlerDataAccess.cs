namespace Fanex.NAP.SPU.AFC.AccountHandlerServices
{
    using Fanex.Data.Repository;
    using Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData;
    using Newtonsoft.Json;
    using System.Collections.Generic;

    public interface IAccountHandlerDataAccess
    {
        IEnumerable<FraudCustomerEntity> GetFraudCases(string custIds);

        IEnumerable<ParticipantEntity> GetParticipant();

        IEnumerable<AccountExistenceEntity> CheckAccountExistence(int? participantID, IEnumerable<AccountExistenceEntity> accountIDs);
    }

    public class AccountHandlerDataAccess : IAccountHandlerDataAccess
    {
        private readonly IDynamicRepository repository;

        public AccountHandlerDataAccess()
        {
            repository = new DynamicRepository();
        }

        public IEnumerable<FraudCustomerEntity> GetFraudCases(string custIds)
        {
            var criteria = new LicenseeCustFilterCriteria
            {
                CustIDs = custIds
            };

            return repository.Fetch<FraudCustomerEntity>(criteria);
        }

        public IEnumerable<ParticipantEntity> GetParticipant()
        {
            var criteria = new ParticipantGetCriteria();

            return repository.Fetch<ParticipantEntity>(criteria);
        }

        public IEnumerable<AccountExistenceEntity> CheckAccountExistence(int? participantID, IEnumerable<AccountExistenceEntity> accountIDs)
        {
            var criteria = new AccountExistenceCheckCriteria
            {
                JsonAccountInfo = JsonConvert.SerializeObject(accountIDs),
                ParticipantID = participantID
            };

            return repository.Fetch<AccountExistenceEntity>(criteria);
        }
    }
}