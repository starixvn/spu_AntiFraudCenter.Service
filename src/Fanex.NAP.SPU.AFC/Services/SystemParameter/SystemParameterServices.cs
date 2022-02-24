namespace Fanex.NAP.SPU.AFC.Services.SystemParameter
{
    using System;
    using static Fanex.NAP.SPU.AFC.Shares.Consts;

    public interface ISystemParameterServices
    {
        Int64 GetLastScannedId(SystemParameters paraID);

        DateTime? GetLastScannedDateTime(SystemParameters paraID);

        bool UpdateTimingExecution(string paraValue, SystemParameters paramId);
    }

    public class SystemParameterServices : ISystemParameterServices
    {
        private readonly ISystemParameterDataAccess systemParameterDataAccess;

        public SystemParameterServices(ISystemParameterDataAccess systemParameterDataAccess)
        {
            this.systemParameterDataAccess = systemParameterDataAccess;
        }

        public Int64 GetLastScannedId(SystemParameters paraID)
        {
            var lastScannedId = systemParameterDataAccess.GetPreviousTimingExecution(paraID);

            return lastScannedId != null ? Int64.Parse(lastScannedId?.ParameterValue) : 0;
        }

        public DateTime? GetLastScannedDateTime(SystemParameters paraID)
        {
            var lastScannedTransID = systemParameterDataAccess.GetPreviousTimingExecution(paraID);

            return lastScannedTransID != null ? DateTime.Parse(lastScannedTransID?.ParameterValue) : null;
        }

        public bool UpdateTimingExecution(string paraValue, SystemParameters paramId)
        {
            if (!string.IsNullOrEmpty(paraValue))
            {
                systemParameterDataAccess.UpdateTimingExecution(paraValue, paramId);
            }

            return true;
        }
    }
}