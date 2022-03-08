namespace Fanex.NAP.SPU.CTS.CC.Features.AntiFraudCenter.Model
{
    using Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData;
    using System.Collections.Generic;
    using static Fanex.NAP.SPU.AFC.Shares.Consts;

    public class B2CAccountResultModel
    {
        public IEnumerable<GetAccountModel> B2CAccounts { get; set; }

        public HttpStatus StatusCode { get; set; }

        public string ErrorMessage { get; set; }
    }
}