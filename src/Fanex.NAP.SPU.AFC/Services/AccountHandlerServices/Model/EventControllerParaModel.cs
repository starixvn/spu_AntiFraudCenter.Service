namespace Fanex.NAP.SPU.AFC.AccountHandlerServices
{
    using System;
    using System.Collections.Generic;
    using static Fanex.NAP.SPU.AFC.Shares.Consts;

    public class EventControllerParaModel
    {
        public IEnumerable<Int64> CustIds { get; set; }

        public FromFlow FromFlow { get; set; }
    }
}