#pragma warning disable CS1591
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Discord.DarthClient.Gateway
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal class IdentifyParams
    {
        [JsonProperty("token")]
        public string Token { get; set; }
        [JsonProperty("properties")]
        public IDictionary<string, string> Properties { get; set; }
        [JsonProperty("large_threshold")]
        public int LargeThreshold { get; set; }
        [JsonProperty("shard")]
        public int[] ShardingParams { get; set; }
        [JsonProperty("guild_subscriptions")]
        public bool GuildSubscriptions { get; set; }
        [JsonProperty("intents")]
        public int Intents { get; set; }
    }
}
