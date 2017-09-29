namespace Entities
{
    using Newtonsoft.Json;

    public class ClaymoreMessageDTO
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("result")]
        public string[] Result { get; set; }
    }
}
