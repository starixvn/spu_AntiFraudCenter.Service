namespace Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData

{
    using Newtonsoft.Json;
    using System;
    using System.Runtime.Serialization;
    public class GetB2CEntity
    {
        [IgnoreDataMember]
        public Int64 CustId { get; set; }

        [JsonProperty(PropertyName = "AccountID")]
        public string B2CCustId { get; set; }
    }
}