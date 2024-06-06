using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;
using TwitchLib.EventSub.Websockets;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TwitchLib.EventSub.Websockets.Core.EventArgs;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Channel;
using Microsoft.Extensions.Hosting;

namespace BluBotCore.Services
{
    public class EventSubHostedService : IHostedService
    {
        private readonly ILogger<EventSubHostedService> _logger;
        private readonly EventSubWebsocketClient _eventSubWebsocketClient;

        public EventSubHostedService(ILogger<EventSubHostedService> logger, EventSubWebsocketClient eventSubWebsocketClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _eventSubWebsocketClient = eventSubWebsocketClient ?? throw new ArgumentNullException(nameof(eventSubWebsocketClient));
            _eventSubWebsocketClient.WebsocketConnected += EventSubWebsocketClient_WebsocketConnected;
            _eventSubWebsocketClient.WebsocketDisconnected += EventSubWebsocketClient_WebsocketDisconnected;
            _eventSubWebsocketClient.WebsocketReconnected += EventSubWebsocketClient_WebsocketReconnected;
            _eventSubWebsocketClient.ErrorOccurred += EventSubWebsocketClient_ErrorOccurred;

            _eventSubWebsocketClient.StreamOffline += EventSubWebsocketClient_StreamOffline;
            _eventSubWebsocketClient.StreamOnline += EventSubWebsocketClient_StreamOnline;
            _eventSubWebsocketClient.ChannelUpdate += EventSubWebsocketClient_ChannelUpdate;
        }

        private async Task EventSubWebsocketClient_ChannelUpdate(object sender, ChannelUpdateArgs args)
        {
            var eventData = args.Notification.Payload.Event;
            await Task.CompletedTask;
        }

        private async Task EventSubWebsocketClient_StreamOnline(object sender, TwitchLib.EventSub.Websockets.Core.EventArgs.Stream.StreamOnlineArgs args)
        {
            var eventData = args.Notification.Payload.Event;
            await Task.CompletedTask;
        }

        private async Task EventSubWebsocketClient_StreamOffline(object sender, TwitchLib.EventSub.Websockets.Core.EventArgs.Stream.StreamOfflineArgs args)
        {
            var eventData = args.Notification.Payload.Event;
            await Task.CompletedTask;
        }

        private async Task EventSubWebsocketClient_ErrorOccurred(object sender, ErrorOccuredArgs args)
        {
            _logger.LogError($"Websocket {_eventSubWebsocketClient.SessionId} - Error occurred!");
            await Task.CompletedTask;
        }

        private async Task EventSubWebsocketClient_WebsocketReconnected(object sender, EventArgs args)
        {
            _logger.LogWarning($"Websocket {_eventSubWebsocketClient.SessionId} reconnected");
            await Task.CompletedTask;
        }

        private async Task EventSubWebsocketClient_WebsocketDisconnected(object sender, EventArgs args)
        {
            _logger.LogError($"Websocket {_eventSubWebsocketClient.SessionId} disconnected!");

            // Don't do this in production. You should implement a better reconnect strategy with exponential backoff
            while (!await _eventSubWebsocketClient.ReconnectAsync())
            {
                _logger.LogError("Websocket reconnect failed!");
                await Task.Delay(1000);
            }
        }

        private async Task EventSubWebsocketClient_WebsocketConnected(object sender, WebsocketConnectedArgs args)
        {
            _logger.LogInformation($"Websocket {_eventSubWebsocketClient.SessionId} connected!");

            if (!args.IsRequestedReconnect)
            {
                // subscribe to topics
            }
            await Task.CompletedTask;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _eventSubWebsocketClient.ConnectAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _eventSubWebsocketClient.DisconnectAsync();
        }


    }
}
