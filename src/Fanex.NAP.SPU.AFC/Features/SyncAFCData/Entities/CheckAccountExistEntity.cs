using System.Text.Json.Serialization;

namespace Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData

{
    public class CheckAccountExistEntity
    {
        public string AccountID { get; set; }

        [JsonIgnore]
        public string Status { get; set; }
    }
}