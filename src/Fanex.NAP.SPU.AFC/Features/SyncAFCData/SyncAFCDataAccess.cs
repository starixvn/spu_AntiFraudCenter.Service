namespace Fanex.NAP.SPU.AFC.Features.SyncAFCData
{
    using Fanex.Data;
    using Fanex.Data.Repository;
    using Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData;
    using System.Collections.Generic;

    public interface ISyncAFCDataAccess
    {
        IEnumerable<CustToSyncAFCEntity> GetCustomerToSyncAFC();

        IEnumerable<GetB2CEntity> GetB2CCustIds(string custIds);

        IEnumerable<GetAFCAccountEntity> GetAFCAccountInfo(string custIdJson);
    }

    public class SyncAFCDataAccess : ISyncAFCDataAccess
    {
        private readonly IDynamicRepository repository;

        public SyncAFCDataAccess()
        {
            repository = new DynamicRepository();
        }

        public IEnumerable<GetAFCAccountEntity> GetAFCAccountInfo(string custIdJson)
        {
            var criteria = new GetAFCAccountInfoCriteria
            {
                B2CCustIdJson = custIdJson
            };

            return repository.Fetch<GetAFCAccountEntity>(criteria);
        }

        public IEnumerable<GetB2CEntity> GetB2CCustIds(string custIds)
        {
            var criteria = new GetB2CCustIdsCriteria
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
    }
}