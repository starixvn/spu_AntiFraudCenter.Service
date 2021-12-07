using System.Collections.Generic;

namespace Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData

{
    public class InsertAccountParasViewEntity
    {
        public string Key { get; set; }

        public IEnumerable<GetAccountViewEntity> Data { get; set; }
    }
}