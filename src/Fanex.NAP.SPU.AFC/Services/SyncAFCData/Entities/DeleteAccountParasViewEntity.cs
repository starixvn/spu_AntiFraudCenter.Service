using System.Collections.Generic;

namespace Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData

{
    public class DeleteAccountParasViewEntity
    {
        public string Key { get; set; }

        public IEnumerable<GetB2CEntity> Data { get; set; }
    }
}