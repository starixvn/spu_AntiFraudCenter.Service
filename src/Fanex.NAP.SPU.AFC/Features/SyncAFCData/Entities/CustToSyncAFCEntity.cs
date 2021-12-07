using System.Collections.Generic;
using System.Linq;

namespace Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData
{
    public class CustToSyncAFCEntity
    {
        public int CustID { get; set; }

        public string FraudID { get; set; }

        public IEnumerable<int> FraudIDList
        {
            get
            {
                if (!string.IsNullOrEmpty(FraudID))
                {
                    return FraudID.Split(',').Select(int.Parse);
                }

                return new List<int>();
            }
        }
    }
}