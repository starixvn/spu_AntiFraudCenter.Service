using Newtonsoft.Json;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData
{
    public class GetAccountViewEntity
    {
        [IgnoreDataMember]
        public int? CustId { get; set; }

        [JsonProperty(PropertyName = "AccountID")]
        public string B2CustId { get; set; }

        [JsonProperty(PropertyName = "CountryCode")]
        public string CountryCode { get; set; }

        [JsonProperty(PropertyName = "Phone")]
        public string PhoneNo { get; set; }

        [JsonProperty(PropertyName = "Email")]
        public string Email { get; set; }

        [JsonProperty(PropertyName = "BankAccountID")]
        public string AccountNum { get; set; }

        [JsonProperty(PropertyName = "BankName")]
        public string AccountName { get; set; }

        [JsonProperty(PropertyName = "FraudID")]
        public IEnumerable<int> FraudID { get; set; }
    }
}