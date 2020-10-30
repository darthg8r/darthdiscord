#pragma warning disable CS1591
using System;
using Newtonsoft.Json;

namespace Discord.DarthClient.Common
{
    public class Message
    {
        [JsonProperty("id")]
        public ulong Id { get; set; }
        [JsonProperty("channel_id")]
        public ulong ChannelId { get; set; }
        // ALWAYS sent on WebSocket messages
        [JsonProperty("guild_id")]
        public ulong GuildId { get; set; }
        [JsonProperty("author")]
        public User Author { get; set; }
        // ALWAYS sent on WebSocket messages
        [JsonProperty("member")]
        public GuildMember Member { get; set; }
        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("attachments")]
        public Attachment[] Attachments { get; set; }

        [JsonProperty("pinned")]
        public bool Pinned { get; set; }

    }
}
