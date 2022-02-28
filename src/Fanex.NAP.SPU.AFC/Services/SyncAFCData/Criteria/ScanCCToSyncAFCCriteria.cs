namespace Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData
{
    using Fanex.Data.Repository;

    public class ScanCCToSyncAFCCriteria : CriteriaBase
    {
        public override string GetSettingKey()
               => "CTS_DC_CustClassification_ProblemAccount_GetB2CByBatch";

        public override bool IsValid()
            => true;
    }
}