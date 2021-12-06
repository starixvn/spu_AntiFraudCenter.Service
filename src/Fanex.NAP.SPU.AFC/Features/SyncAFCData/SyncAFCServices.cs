namespace Fanex.NAP.SPU.AFC.Features.SyncAFCData
{
    using Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Linq;

    public interface ISyncAFCServices
    {
        IEnumerable<CustToSyncAFCEntity> GetCustomerToSyncAFC();

        IEnumerable<GetAFCAccountEntity> GetB2CInfo(string custIds);
    }

    public class SyncAFCServices : ISyncAFCServices
    {
        private readonly ISyncAFCDataAccess syncAFCDataAccess;

        public SyncAFCServices(ISyncAFCDataAccess syncAFCDataAccess)
        {
            this.syncAFCDataAccess = syncAFCDataAccess;
        }

        public IEnumerable<CustToSyncAFCEntity> GetCustomerToSyncAFC()
        {
            return syncAFCDataAccess.GetCustomerToSyncAFC();
        }

        public IEnumerable<GetAFCAccountEntity> GetB2CInfo(string custIds)
        {
            if (!string.IsNullOrEmpty(custIds))
            {
                var b2cCustIds = syncAFCDataAccess.GetB2CCustIds(custIds);

                if (b2cCustIds != null && b2cCustIds.Any())
                {
                    var b2cCustJson = JsonConvert.SerializeObject(b2cCustIds.Select(c => c.B2CCustId));

                    return syncAFCDataAccess.GetAFCAccountInfo(b2cCustJson) ?? new List<GetAFCAccountEntity>();
                }
            }

            return new List<GetAFCAccountEntity>();
        }
    }
}