namespace Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData
{
    using System.Collections.Generic;

    public class GetAccountModel
    {
        //B2CustId
        public string B2CCustId { get; set; }

        public string CountryCode { get; set; }

        public string Phone { get; set; }

        public string Email { get; set; }

        public string BankAccountID { get; set; }

        public string BankName { get; set; }

        public string FullName { get; set; }

        public string DocumentID { get; set; }

        public string DocumentTypeID { get; set; }

        public IEnumerable<int> FraudID { get; set; }
    }
}