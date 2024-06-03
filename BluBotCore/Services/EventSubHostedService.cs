using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.EventSub.Webhooks.Core;
using TwitchLib.EventSub.Webhooks.Core.EventArgs;
using TwitchLib.EventSub.Webhooks.Core.EventArgs.Channel;

namespace BluBotCore.Services
{
    public class EventSubHostedService :IHostedService
    {
        private readonly ILogger<EventSubHostedService> _logger;
        private readonly IEventSubWebhooks _eventSubWebhooks;

        public EventSubHostedService(ILogger<EventSubHostedService> logger, IEventSubWebhooks eventSubWebhooks)
        {
            _logger = logger;
            _eventSubWebhooks = eventSubWebhooks;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _eventSubWebhooks.OnError += OnError;
            _eventSubWebhooks.OnStreamOnline += _eventSubWebhooks_OnStreamOnline;
            _eventSubWebhooks.OnStreamOffline += _eventSubWebhooks_OnStreamOffline;
            _eventSubWebhooks.OnChannelUpdate += _eventSubWebhooks_OnChannelUpdate;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _eventSubWebhooks.OnError -= OnError;
            _eventSubWebhooks.OnStreamOnline -= _eventSubWebhooks_OnStreamOnline;
            _eventSubWebhooks.OnStreamOffline -= _eventSubWebhooks_OnStreamOffline;
            _eventSubWebhooks.OnChannelUpdate -= _eventSubWebhooks_OnChannelUpdate;
            return Task.CompletedTask;
        }

        private void _eventSubWebhooks_OnStreamOffline(object sender, TwitchLib.EventSub.Webhooks.Core.EventArgs.Stream.StreamOfflineArgs e)
        {
            throw new System.NotImplementedException();
        }

        private void _eventSubWebhooks_OnStreamOnline(object sender, TwitchLib.EventSub.Webhooks.Core.EventArgs.Stream.StreamOnlineArgs e)
        {
            // e.Notification.Event.BroadcasterUserId
            // e.Notification.Event.Type "live"
            // e.Notification.Event.StartedAt
            throw new System.NotImplementedException();
        }

        private void _eventSubWebhooks_OnChannelUpdate(object sender, ChannelUpdateArgs e)
        {
            //e.Notification.
            throw new System.NotImplementedException();
        }

        private void OnError(object sender, OnErrorArgs e)
        {
            _logger.LogError($"Reason: {e.Reason} - Message: {e.Message}");
        }
    }
}
