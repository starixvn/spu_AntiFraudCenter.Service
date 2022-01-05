namespace Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData
{
    using Newtonsoft.Json;

    public class AccountDBResult
    {
        [JsonProperty(PropertyName = "StatusCode")]
        public int StatusCode { get; set; }

        [JsonProperty(PropertyName = "ErrorMessage")]
        public string ErrorMessage { get; set; }
    }
}