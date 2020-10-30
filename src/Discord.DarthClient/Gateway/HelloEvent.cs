using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Discord.DarthClient.Gateway
{
    internal class HelloEvent
    {
        [JsonProperty("heartbeat_interval")]
        public int HeartbeatInterval { get; set; }
    }
}
