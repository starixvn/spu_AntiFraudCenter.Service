namespace Fanex.NAP.SPU.AFC.Services.TriggerSyncAFC
{
    using Fanex.Data.Repository;
    using Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData;
    using System.Collections.Generic;

    public interface ITriggerSyncAFCDataAccess
    {
        IEnumerable<CustToSyncAFCEntity> GetFraudCases(string custIds);
    }

    public class TriggerSyncAFCDataAccess : ITriggerSyncAFCDataAccess
    {
        private readonly IDynamicRepository repository;

        public TriggerSyncAFCDataAccess()
        {
            repository = new DynamicRepository();
        }

        public IEnumerable<CustToSyncAFCEntity> GetFraudCases(string custIds)
        {
            var criteria = new LicenseeCustFilterCriteria
            {
                CustIDs = custIds
            };

            return repository.Fetch<CustToSyncAFCEntity>(criteria);
        }
    }
}