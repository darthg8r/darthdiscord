using System.Collections.Generic;

namespace DiscordMonitor
{
	public class DiscordOptions 
	{
		public DiscordOptions()
		{
			ChannelMappings = new List<ChannelMapping>();
		}

		public string DiscordToken { get; set; }

		public List<ChannelMapping> ChannelMappings { get; set; }
	}
}
