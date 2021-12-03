namespace Fanex.NAP.SPU.AFC.Features.SystemParameter
{
    using Fanex.Data;
    using Fanex.Data.Repository;
    using Fanex.NAP.SPU.AFC.CC.Features.SystemParameter;
    using Fanex.NAP.SPU.CTS.AFC.Features.SystemParameter;
    using static Fanex.NAP.SPU.AFC.Shares.Consts;

    public interface ISystemParameterDataAccess
    {
        SystemParameterEntity GetPreviousTimingExecution(SystemParameters paramID);

        void UpdateTimingExecution(string paraValue, SystemParameters paramId);
    }

    public class SystemParameterDataAccess : ISystemParameterDataAccess
    {
        private readonly IDynamicRepository repository;

        public SystemParameterDataAccess()
        {
            repository = new DynamicRepository();
        }

        public SystemParameterEntity GetPreviousTimingExecution(SystemParameters paramID)
        {
            var criteria = new PreviousTimingExecutionGetCriteria
            {
                ParamID = (int)paramID
            };

            using (var db = ObjectDbFactory.CreateInstance(criteria.GetSettingKey()))
            {
                return db.QueryEntity<SystemParameterEntity>(criteria);
            }
        }

        public void UpdateTimingExecution(string paraValue, SystemParameters paramId)
        {
            var criteria = new PreviousTimingExecutionUpdateCriteria
            {
                ParamId = (int)paramId,
                ParamValue = paraValue
            };

            repository.Execute(criteria);
        }
    }
}