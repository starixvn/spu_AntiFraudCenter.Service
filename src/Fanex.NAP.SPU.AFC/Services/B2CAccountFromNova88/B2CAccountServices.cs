namespace Fanex.NAP.SPU.AFC.Services.SyncAFCData
{
    using Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData;
    using Fanex.NAP.SPU.AFC.Shares;
    using Fanex.NAP.SPU.CTS.CC.Features.AntiFraudCenter.Model;
    using Newtonsoft.Json;
    using Sentry;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using static Fanex.NAP.SPU.AFC.Shares.Consts;

    public interface IB2CAccountServices
    {
        Task<IEnumerable<GetAccountModel>> GetB2CAccountFromNova88(IEnumerable<string> b2cCustIds, string url);
    }

    public class B2CAccountServices : IB2CAccountServices
    {
        private readonly ISyncAFCDataAccess syncAFCDataAccess;
        private readonly ISentryClient sentryClient;
        private readonly IErrorHandleServices errorHandleServices;
        private const string ServiceName = "Service AntiFraudCenter";

        public B2CAccountServices(IErrorHandleServices errorHandleServices, ISentryClient sentryClient)
        {
            this.errorHandleServices = errorHandleServices;
            this.sentryClient = sentryClient;
        }

        public async Task<IEnumerable<GetAccountModel>> GetB2CAccountFromNova88(IEnumerable<string> b2cCustIds, string url)
        {
            try
            {
                if (b2cCustIds != null && b2cCustIds.Any())
                {
                    var result = await ApiHelper.Post<B2CAccountResultModel>(url, b2cCustIds, 200);

                    if (result.StatusCode == HttpStatus.OK)
                    {
                        return result.B2CAccounts;
                    }

                    Utilities.LogSentryError(ServiceName, $"CTS.CC Service - Get B2CAccounts from AFC.Nova88.API  - Exception", b2cCustIds, new Exception(result.ErrorMessage), sentryClient);
                    errorHandleServices.InsertErrorLog(null, result.ErrorMessage, JsonConvert.SerializeObject(b2cCustIds));
                }
            }
            catch (Exception ex)
            {
                Utilities.LogSentryError(ServiceName, $"CTS.CC Service - Get B2CAccounts from AFC.Nova88.API  - Exception", b2cCustIds, ex, sentryClient);
                errorHandleServices.InsertErrorLog(null, ex.Message, JsonConvert.SerializeObject(b2cCustIds));
            }

            return Enumerable.Empty<GetAccountModel>();
        }

        #region Private methods



        #endregion Private methods
    }
}