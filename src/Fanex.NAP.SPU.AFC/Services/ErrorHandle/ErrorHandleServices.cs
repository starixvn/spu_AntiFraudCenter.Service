namespace Fanex.NAP.SPU.AFC.Services.SyncAFCData
{
    public interface IErrorHandleServices
    {
        void InsertErrorLog(int? participantID, string errorMessage, string dataJson);
    }

    public class ErrorHandleServices : IErrorHandleServices
    {
        private readonly IErrorHandleDataAccess errorHandleDataAccess;

        public ErrorHandleServices(IErrorHandleDataAccess errorHandleDataAccess)
        {
            this.errorHandleDataAccess = errorHandleDataAccess;
        }

        public void InsertErrorLog(int? participantID, string errorMessage, string dataJson)
        {
            errorHandleDataAccess.InsertErrorLog(participantID, errorMessage, dataJson);
        }
    }
}