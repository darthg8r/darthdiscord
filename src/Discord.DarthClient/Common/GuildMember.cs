#pragma warning disable CS1591
using Newtonsoft.Json;

namespace Discord.DarthClient.Common
{
    public class GuildMember
    {
        [JsonProperty("user")]
        public User User { get; set; }
    }
}
