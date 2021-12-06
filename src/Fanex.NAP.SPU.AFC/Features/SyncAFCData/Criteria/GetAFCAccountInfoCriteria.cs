namespace Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData
{
    using Fanex.Data;
    using Fanex.Data.Repository;

    public class GetAFCAccountInfoCriteria : CriteriaBase
    {
        [SpParam(SqlParamName = "CustIdList")]
        public string B2CCustIdJson { get; set; }

        public override string GetSettingKey()
        {
            return "API_CustomerExtraInfo_Get";
        }

        public override bool IsValid()
        {
            return true;
        }
    }
}
