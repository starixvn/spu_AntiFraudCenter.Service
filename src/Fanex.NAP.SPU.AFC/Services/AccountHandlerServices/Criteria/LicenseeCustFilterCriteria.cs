namespace Fanex.NAP.SPU.AFC.AccountHandlerServices
{
    using Fanex.Data;
    using Fanex.Data.Repository;

    public class LicenseeCustFilterCriteria : CriteriaBase
    {
        [SpParam(SqlParamName = "ip_CustIDs")]
        public string CustIDs { get; set; }

        public override string GetSettingKey()
        {
            return "CTS_DC_CustClassification_ProblemAccount_FilterByB2C";
        }

        public override bool IsValid()
        {
            return true;
        }
    }
}