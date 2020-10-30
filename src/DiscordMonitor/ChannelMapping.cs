namespace DiscordMonitor
{
	public class ChannelMapping
	{
		public string Name { get; set; }

		public ulong ServerId { get; set; }

		public ulong ChannelId { get; set; }

		public string Target { get; set; }
	}
}