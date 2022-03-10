namespace Fanex.NAP.SPU.AFC.Services.SyncAFCData
{
    using Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData;
    using Fanex.NAP.SPU.AFC.Shares;
    using Fanex.NAP.SPU.CTS.CC.Features.AntiFraudCenter.Model;
    using global::SPU.AntiFraudCenter.Nova88.Services.Cryptographic;
    using Newtonsoft.Json;
    using Sentry;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using static Fanex.NAP.SPU.AFC.Shares.Consts;

    public interface IB2CAccountServices
    {
        Task<IEnumerable<GetAccountModel>> GetAccountFromNova88(IEnumerable<string> b2cCustIds, IEnumerable<Int64> custIds, string url, FromFlow fromFlow, int environment);
    }

    public class B2CAccountServices : IB2CAccountServices
    {
        private readonly ISentryClient sentryClient;
        private readonly IErrorHandleServices errorHandleServices;
        private const string ServiceName = "AntiFraudCenter";

        public B2CAccountServices(IErrorHandleServices errorHandleServices, ISentryClient sentryClient)
        {
            this.errorHandleServices = errorHandleServices;
            this.sentryClient = sentryClient;
        }

        public async Task<IEnumerable<GetAccountModel>> GetAccountFromNova88(IEnumerable<string> b2cCustIds, IEnumerable<Int64> custIds, string url, FromFlow fromFlow, int environment)
        {
            try
            {
                if (b2cCustIds != null && b2cCustIds.Any())
                {
                    var data = new
                    {
                        Signture = CryptographicHelper.CreateMD5Hash(JsonConvert.SerializeObject(b2cCustIds) + CryptographicConfig.MD5Salt),
                        B2CCustIds = b2cCustIds
                    };

                    var result = await ((environment == 1) ? ApiHelper.Post<B2CAccountResultModel>(url, data) : ApiHelper.PostWithProxy<B2CAccountResultModel>(url, data, 200));

                    if (result.StatusCode == HttpStatus.OK)
                    {
                        return result.B2CAccounts;
                    }

                    Utilities.LogSentryError(ServiceName, $"{fromFlow} - Get B2CAccounts from AFC.Nova88.API Fail", custIds, new Exception(result.ErrorMessage), sentryClient, "CustIDs");
                    errorHandleServices.InsertErrorLog(null, $"{fromFlow} - {result.ErrorMessage}", JsonConvert.SerializeObject(b2cCustIds));
                }
            }
            catch (Exception ex)
            {
                Utilities.LogSentryError(ServiceName, $"{fromFlow} - Get B2CAccounts from AFC.Nova88.API Fail", custIds, ex, sentryClient, "CustIDs");
                errorHandleServices.InsertErrorLog(null, $"{fromFlow} - {ex.Message}", JsonConvert.SerializeObject(b2cCustIds));
            }

            return Enumerable.Empty<GetAccountModel>();
        }
    }
}