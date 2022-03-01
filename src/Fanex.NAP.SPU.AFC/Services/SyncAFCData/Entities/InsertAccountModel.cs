namespace Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    public class InsertAccountModel
    {
        [IgnoreDataMember]
        public Int64 CustId { get; set; }

        [JsonProperty(PropertyName = "AccountID")]
        public string B2CCustId { get; set; }

        [JsonProperty(PropertyName = "CountryCode")]
        public string CountryCode { get; set; }

        [JsonProperty(PropertyName = "Phone")]
        public string Phone { get; set; }

        [JsonProperty(PropertyName = "Email")]
        public string Email { get; set; }

        [JsonProperty(PropertyName = "FullName")]
        public string FullName { get; set; }

        [JsonProperty(PropertyName = "DocumentID")]
        public string DocumentID { get; set; }

        [JsonProperty(PropertyName = "DocumentTypeID")]
        public int? DocumentTypeID { get; set; }

        [JsonProperty(PropertyName = "BankInfo")]
        public IEnumerable<BankAccountModel> BankAccount { get; set; }

        [JsonProperty(PropertyName = "FraudID")]
        public IEnumerable<int> FraudID { get; set; }
    }
}