namespace Fanex.NAP.SPU.AFC.Services.TriggerSyncAFC.Model
{
    using System;
    using System.Collections.Generic;
    using static Fanex.NAP.SPU.AFC.Shares.Consts;

    public class EventParameterModel
    {
        public IEnumerable<Int64> CustIds { get; set; }

        public FromFlow FromFlow { get; set; }
    }
}
