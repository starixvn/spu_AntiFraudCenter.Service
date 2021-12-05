namespace Fanex.NAP.SPU.AFC.Features.SyncAFCData
{
    using Fanex.Data;
    using Fanex.Data.Repository;
    using Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData;

    public interface ISyncAFCDataAccess
    {
        CustToSyncAFCEntity GetCustomerToSyncAFC();
    }

    public class SyncAFCDataAccess : ISyncAFCDataAccess
    {
        private readonly IDynamicRepository repository;

        public SyncAFCDataAccess()
        {
            repository = new DynamicRepository();
        }

        public CustToSyncAFCEntity GetCustomerToSyncAFC()
        {
            var criteria = new CustToSyncAFCGet();

            using (var db = ObjectDbFactory.CreateInstance(criteria.GetSettingKey()))
            {
                return db.QueryEntity<CustToSyncAFCEntity>(criteria);
            }
        }
    }
}