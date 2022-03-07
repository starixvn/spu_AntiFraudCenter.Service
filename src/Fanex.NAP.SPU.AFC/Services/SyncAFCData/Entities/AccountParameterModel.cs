namespace Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData
{
    using Fanex.NAP.SPU.AFC.Shares;
    using Fanex.NAP.SPU.AFC.Shares.ValidationData;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    public class AccountParameterModel
    {
        [IgnoreDataMember]
        public Int64 CustId { get; set; }

        [JsonProperty(PropertyName = "AccountID")]
        public string B2CCustId { get; set; }

        [JsonProperty(PropertyName = "CountryCode")]
        public string CountryCode { get; set; }

        [JsonProperty(PropertyName = "Phone")]
        [MaxLength(Consts.MaxLenght)]
        [RequiredDependency("CountryCode")]
        [RequiredAtLeast("Email", "DocumentID", "BankAccountID")]
        public string Phone { get; set; }

        [JsonProperty(PropertyName = "Email")]
        public string Email { get; set; }

        [JsonProperty(PropertyName = "BankAccountID")]
        public string BankAccountID { get; set; }

        [JsonProperty(PropertyName = "BankName")]
        public string BankName { get; set; }

        [JsonProperty(PropertyName = "FullName")]
        public string FullName { get; set; }

        [JsonProperty(PropertyName = "DocumentID")]
        [MaxLength(Consts.MaxLenght)]
        public string DocumentID { get; set; }

        [JsonProperty(PropertyName = "DocumentTypeID")]
        public int? DocumentTypeID { get; set; }

        [JsonProperty(PropertyName = "FraudID")]
        public IEnumerable<int> FraudID { get; set; }
    }
}