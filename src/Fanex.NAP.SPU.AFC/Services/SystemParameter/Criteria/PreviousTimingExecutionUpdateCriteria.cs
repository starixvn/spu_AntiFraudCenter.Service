namespace Fanex.NAP.SPU.CTS.AFC.Features.SystemParameter
{
    using Fanex.Data;
    using Fanex.Data.Repository;

    public class PreviousTimingExecutionUpdateCriteria : NonQueryCommand
    {
        [SpParam(SqlParamName = "ip_ParamID")]
        public int ParamId { get; set; }

        [SpParam(SqlParamName = "ip_ParamValue")]
        public string ParamValue { get; set; }

        public override string GetSettingKey()
        {
            return "CTS_DC_SystemParameter_Update";
        }

        public override bool IsValid()
        {
            return true;
        }
    }
}