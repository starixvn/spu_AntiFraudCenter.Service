namespace Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData
{
    using Fanex.Data;
    using Fanex.Data.Repository;

    public class AccountCheckExistCriteria : CriteriaBase
    {
        [SpParam(SqlParamName = "ip_ParticipantID")]
        public int? ParticipantID { get; set; }

        [SpParam(SqlParamName = "ip_AccountInfo")]
        public string JsonAccountInfo { get; set; }

        public override string GetSettingKey()
        {
            return "AFC_DC_Account_CheckExist";
        }

        public override bool IsValid()
        {
            return true;
        }
    }
}