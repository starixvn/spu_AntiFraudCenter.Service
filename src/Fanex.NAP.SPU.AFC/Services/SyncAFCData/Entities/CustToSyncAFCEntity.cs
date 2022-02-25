namespace Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class CustToSyncAFCEntity
    {
        public Int64 CustID { get; set; }

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