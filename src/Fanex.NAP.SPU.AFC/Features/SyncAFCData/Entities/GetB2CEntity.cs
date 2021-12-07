using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData

{
    public class GetB2CEntity
    {
        [IgnoreDataMember]
        public int CustId { get; set; }

        [JsonProperty(PropertyName = "AccountID")]
        public string B2CCustId { get; set; }
    }
}