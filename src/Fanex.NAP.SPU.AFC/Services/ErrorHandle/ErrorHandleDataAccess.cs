namespace Fanex.NAP.SPU.AFC.Services.SyncAFCData
{
    using Fanex.Data.Repository;
    using Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData;

    public interface IErrorHandleDataAccess
    {
        void InsertErrorLog(int? participantID, string errorMessage, string dataJson);
    }

    public class ErrorHandleDataAccess : IErrorHandleDataAccess
    {
        private readonly IDynamicRepository repository;

        public ErrorHandleDataAccess()
        {
            repository = new DynamicRepository();
        }

        public void InsertErrorLog(int? participantID, string errorMessage, string dataJson)
        {
            var criteria = new ErrorLogInsertCriteria
            {
                ErrorMessage = errorMessage,
                DataJson = dataJson,
                ParticipantID = participantID
            };

            repository.Execute(criteria);
        }
    }
}