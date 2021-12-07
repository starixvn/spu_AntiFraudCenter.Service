namespace Fanex.NAP.SPU.AFC.Features.SyncAFCData
{
    using Fanex.Data.Repository;
    using Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData;
    using Newtonsoft.Json;
    using System.Collections.Generic;

    public interface ISyncAFCDataAccess
    {
        IEnumerable<GetAccountEntity> GetAccountInfo(string custIdJson);

        IEnumerable<GetB2CEntity> GetB2CustIds(string custIds);

        IEnumerable<CheckAccountExistEntity> CheckAccountValidation(int? participantID, IEnumerable<CheckAccountExistEntity> accountIDs);

        IEnumerable<ParticipantEntity> GetParticipant();

        IEnumerable<CustToSyncAFCEntity> GetCustomerToSyncAFC();
    }

    public class SyncAFCDataAccess : ISyncAFCDataAccess
    {
        private readonly IDynamicRepository repository;

        public SyncAFCDataAccess()
        {
            repository = new DynamicRepository();
        }

        public IEnumerable<GetAccountEntity> GetAccountInfo(string custIdJson)
        {
            var criteria = new GetAccountInfoCriteria
            {
                B2CCustIdJson = custIdJson
            };

            return repository.Fetch<GetAccountEntity>(criteria);
        }

        public IEnumerable<GetB2CEntity> GetB2CustIds(string custIds)
        {
            var criteria = new GetB2CustIdsCriteria
            {
                CustIds = custIds
            };

            return repository.Fetch<GetB2CEntity>(criteria);
        }

        public IEnumerable<CustToSyncAFCEntity> GetCustomerToSyncAFC()
        {
            var criteria = new GetCustToSyncAFCCriteria();

            return repository.Fetch<CustToSyncAFCEntity>(criteria);
        }

        public IEnumerable<CheckAccountExistEntity> CheckAccountValidation(int? participantID, IEnumerable<CheckAccountExistEntity> accountIDs)
        {
            var criteria = new AccountCheckExistCriteria
            {
                JsonAccountInfo = JsonConvert.SerializeObject(accountIDs),
                ParticipantID = participantID
            };

            return repository.Fetch<CheckAccountExistEntity>(criteria);
        }

        public IEnumerable<ParticipantEntity> GetParticipant()
        {
            var criteria = new ParticipantGetCriteria();

            return repository.Fetch<ParticipantEntity>(criteria);
        }
    }
}