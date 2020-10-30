using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord.DarthClient.Common;
using Discord.DarthClient.Gateway;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Discord.DarthClient
{
	public class WebsocketClient
	{
		private readonly string _token;
		private readonly ILogger<WebsocketClient> _logger;
		private readonly Websocket.Client.WebsocketClient websocketClient;
		private readonly JsonSerializer _serializer;
		private int _lastSeq;
		private CancellationToken _cancellationToken;
		private LoginState _loginState;

		// ReSharper disable once NotAccessedField.Local
		private Task _heartbeatTask;

		public event EventHandler<Message> MessageCreatedEvent;
		public event EventHandler<Message> MessageUpdatedEvent;
		public event EventHandler DisconnectedEvent;

		protected virtual void OnMessageCreatedEvent(Message message)
		{
			MessageCreatedEvent?.Invoke(this, message);
		}

		protected virtual void OnMessageUpdatedEvent(Message message)
		{
			MessageUpdatedEvent?.Invoke(this, message);

		}

		protected virtual void OnDisconnectedEvent()
		{

			DisconnectedEvent?.Invoke(this, EventArgs.Empty);

		}

		public WebsocketClient(string token, ILogger<WebsocketClient> logger)
		{
			_token = token;
			_logger = logger;
			_heartbeatTimes = new ConcurrentQueue<long>();
			websocketClient = new Websocket.Client.WebsocketClient(new Uri("wss://gateway.discord.gg/?v=6&encoding=json"));
			websocketClient.IsReconnectionEnabled = true;
			_cancellationToken = new CancellationToken();
			_serializer = new JsonSerializer();
		}

		private async Task ProcessMessageAsync(GatewayOpCode opCode, int? seq, string type, object payload)
		{
			if (seq != null)
				_lastSeq = seq.Value;
			_lastMessageTime = Environment.TickCount;

			try
			{
				switch (opCode)
				{
					case GatewayOpCode.Hello:
						{
							_logger.LogDebug("Received Hello");
							var data = (payload as JToken).ToObject<HelloEvent>();
							_heartbeatTask = RunHeartbeatAsync(data.HeartbeatInterval, _cancellationToken);
						}
						break;
					case GatewayOpCode.InvalidSession:
						_logger.LogDebug("InvalidSession");
						break;

					case GatewayOpCode.Identify:
						_logger.LogDebug("InvalidSession");
						break;
					case GatewayOpCode.Heartbeat:
						_logger.LogDebug("Heartbeat");
						websocketClient.Send(SerializeJson(new SocketFrame()
						{
							Operation = (int)GatewayOpCode.HeartbeatAck
						}));
						break;
					case GatewayOpCode.HeartbeatAck:
						_heartbeatTimes.TryDequeue(out long time);
						break;
					case GatewayOpCode.Dispatch:
						_logger.LogDebug("Dispatch");

						switch (type)
						{
							//Messages
							case "MESSAGE_CREATE":
								var messageCreated = (payload as JToken).ToObject<Message>(_serializer);
								OnMessageCreatedEvent(messageCreated);
								break;
							case "MESSAGE_UPDATE":
								var messageUpdated = (payload as JToken).ToObject<Message>(_serializer);
								OnMessageUpdatedEvent(messageUpdated);
								break;
						}

						break;
					default:
						_logger.LogDebug(opCode.ToString());
						break;
				}
			}
			catch (Exception ex)
			{

			}
		}

		private async Task Setup()
		{
			await Task.Delay(2000);
			var identify = new IdentifyParams();
			var dataFrame = new SocketFrame();
			dataFrame.Operation = (int)GatewayOpCode.Identify;
			dataFrame.Payload = identify;
			identify.Token = _token;
			identify.GuildSubscriptions = false;
			identify.ShardingParams = new[] { 0, 1 };
			identify.Properties = new Dictionary<string, string>
			{
				{"$os", "linux"}, {"$browser", "DarthDiscord"}, {"device", "DarthDiscord"}
			};
			identify.LargeThreshold = 50;
			identify.Intents = (int)GatewayIntents.GuildMessages;
			var dataFrameJson = SerializeJson(dataFrame);
			websocketClient.Send(dataFrameJson);
		}

		public async Task Start()
		{
			websocketClient.ReconnectionHappened.Subscribe(async (x) =>
			{
				await Setup();
			});


			websocketClient.DisconnectionHappened.Subscribe(msg =>
			{
				_logger.LogInformation($"Disconnected, {msg.CloseStatusDescription}");
				OnDisconnectedEvent();
			});
			websocketClient.MessageReceived.Subscribe(async msg =>
			{
				var frame = Newtonsoft.Json.JsonConvert.DeserializeObject<SocketFrame>(msg.Text);
				var msgPretty = Newtonsoft.Json.JsonConvert.SerializeObject(frame, Formatting.Indented);
				_logger.LogTrace(msgPretty);

				await ProcessMessageAsync((GatewayOpCode)frame.Operation, frame.Sequence, frame.Type, frame.Payload);
			});

			await websocketClient.Start();


		}

		private readonly ConcurrentQueue<long> _heartbeatTimes;
		private long _lastMessageTime;


		private Task SendHeartbeatAsync(int lastSequence)
		{
			var frame = new SocketFrame();
			frame.Operation = (int)GatewayOpCode.Heartbeat;
			frame.Payload = _lastSeq == 0 ? (int?)null : _lastSeq;

			websocketClient.Send(SerializeJson(frame));

			return Task.CompletedTask;
		}

		protected string SerializeJson(object value)
		{
			var sb = new StringBuilder(256);
			using (TextWriter text = new StringWriter(sb, CultureInfo.InvariantCulture))
			using (JsonWriter writer = new JsonTextWriter(text))
				_serializer.Serialize(writer, value);
			return sb.ToString();
		}

		protected void CheckState()
		{
			if (_loginState != LoginState.LoggedIn)
				throw new InvalidOperationException("Client is not logged in.");
		}

		//Core
		internal Task SendGatewayAsync(GatewayOpCode opCode, object payload)
			=> SendGatewayInternalAsync(opCode, payload);


		private async Task SendGatewayInternalAsync(GatewayOpCode opCode, object payload)
		{
			CheckState();

			//TODO: Add ETF
			byte[] bytes = null;
			payload = new SocketFrame { Operation = (int)opCode, Payload = payload };


			if (payload != null)
				bytes = Encoding.UTF8.GetBytes(SerializeJson(payload));

			websocketClient.Send(bytes);
		}

		private async Task RunHeartbeatAsync(int intervalMillis, CancellationToken cancelToken)
		{
			try
			{
				_logger.LogDebug("Heartbeat Started");
				while (!cancelToken.IsCancellationRequested)
				{
					int now = Environment.TickCount;

					//Did server respond to our last heartbeat, or are we still receiving messages (long load?)
					if (_heartbeatTimes.Count != 0)
					{
						if (websocketClient.IsStarted)
						{
							_logger.LogInformation("Missed heartbeat");
							// await websocketClient.Stop(System.Net.WebSockets.WebSocketCloseStatus.ProtocolError, "Missed Heartbeat");
							return;
							// Do something because of missed heartbeat
						}
					}

					_heartbeatTimes.Enqueue(now);
					try
					{
						await SendHeartbeatAsync(_lastSeq).ConfigureAwait(false);
					}
					catch (Exception ex)
					{
						_logger.LogDebug("Heartbeat Errored", ex);
					}

					await Task.Delay(intervalMillis, cancelToken).ConfigureAwait(false);
				}
				_logger.LogDebug("Heartbeat Stopped");
			}
			catch (OperationCanceledException)
			{
				_logger.LogDebug("Heartbeat Stopped");
			}
			catch (Exception ex)
			{
				_logger.LogError("Heartbeat Errored", ex);
			}
		}

	}
}
