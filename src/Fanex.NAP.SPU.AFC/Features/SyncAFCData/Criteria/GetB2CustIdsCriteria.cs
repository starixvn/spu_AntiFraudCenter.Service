namespace Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData
{
    using Fanex.Data;
    using Fanex.Data.Repository;

    public class GetB2CustIdsCriteria : CriteriaBase
    {
        [SpParam(SqlParamName = "CustIds")]
        public string CustIds { get; set; }

        public override string GetSettingKey()
        {
            return "AFC_Get_B2CCustId";
        }

        public override bool IsValid()
        {
            return true;
        }
    }
}