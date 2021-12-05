namespace Fanex.NAP.SPU.AFC.Features.SyncAFCData
{
    using Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData;

    public interface ISyncAFCServices
    {
        CustToSyncAFCEntity GetCustomerToSyncAFC();
    }

    public class SyncAFCServices : ISyncAFCServices
    {
        private readonly ISyncAFCDataAccess syncAFCDataAccess;

        public SyncAFCServices(ISyncAFCDataAccess syncAFCDataAccess)
        {
            this.syncAFCDataAccess = syncAFCDataAccess;
        }

        public CustToSyncAFCEntity GetCustomerToSyncAFC()
        {
            return syncAFCDataAccess.GetCustomerToSyncAFC();
        }
    }
}