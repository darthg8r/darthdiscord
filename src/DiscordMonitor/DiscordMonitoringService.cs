using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord.DarthClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordMonitor
{
	public class DiscordMonitoringService : IHostedService
	{
		private readonly IOptions<DiscordOptions> _options;
		private readonly ILoggerFactory _loggerFactory;
		private readonly ILogger<DiscordMonitoringService> _logger;
		private WebsocketClient _discordSocketClient;


		public DiscordMonitoringService(IOptions<DiscordOptions> options, ILoggerFactory loggerFactory, ILogger<DiscordMonitoringService> logger)
		{
			_options = options;
			_loggerFactory = loggerFactory;
			_logger = logger;
		}



		public async Task StartAsync(CancellationToken cancellationToken)
		{
			var token = _options.Value.DiscordToken;

			_discordSocketClient = new WebsocketClient(token, _loggerFactory.CreateLogger<WebsocketClient>());

			_discordSocketClient.MessageCreatedEvent += _discordSocketClient_MessageCreatedEvent; 

			await _discordSocketClient.Start();
		}





		private async void _discordSocketClient_MessageCreatedEvent(object sender, Discord.DarthClient.Common.Message e)
		{
			try
			{
				HttpClient client = new HttpClient();

				foreach (var mapping in _options.Value.ChannelMappings)
				{
					if (mapping.ServerId == e.GuildId && e.ChannelId == mapping.ChannelId)
					{
						if (!string.IsNullOrWhiteSpace(mapping.Target))
						{
							var message = $"{e.Content} {e.Attachments.FirstOrDefault()?.Url}";
							await client.PostAsJsonAsync(mapping.Target,
								new
								{
									content = message
								});
						}
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex.ToString());
			}

		}



		public Task StopAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}
	}
}