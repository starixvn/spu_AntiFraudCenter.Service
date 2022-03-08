namespace Fanex.NAP.SPU.AFC.AccountHandlerServices
{
    using System.Text.Json.Serialization;

    public class AccountExistenceEntity
    {
        public string AccountID { get; set; }

        [JsonIgnore]
        public string Status { get; set; }
    }
}