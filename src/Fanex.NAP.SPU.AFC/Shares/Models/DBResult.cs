namespace Fanex.NAP.SPU.AFC.Shares.Models
{
    using Newtonsoft.Json;
    using System.Collections;

    public class DBResult
    {
        public int ErrorCode { get; set; }

        public string ErrorMsg { get; set; }

        [JsonIgnore]
        public Hashtable OutParam { get; set; }
    }
}