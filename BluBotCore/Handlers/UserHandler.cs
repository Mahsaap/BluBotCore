using BluBotCore.Other;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace BluBotCore.Handlers
{
    class UserHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _service;
        public UserHandler(IServiceProvider service, DiscordSocketClient client)
        {
            _client = client;
            _service = service;

            _client.UserJoined += Client_UserJoined;
            _client.UserLeft += Client_UserLeft;
            _client.UserBanned += Client_UserBanned;
            _client.UserUnbanned += Client_UserUnbanned;
            //_client.UserVoiceStateUpdated += _client_UserVoiceStateUpdated;
        }

        private async Task Client_UserVoiceStateUpdated(SocketUser user, SocketVoiceState voiceBefore, SocketVoiceState voiceAfter)
        {
            if (Setup.DiscordLogChannel == 0) return;
            //Joined Voice Channel
            if (voiceBefore.VoiceChannel == null && voiceAfter.VoiceChannel != null)
            {
                var time = DateTime.Now.ToString("HH:MM:ss");
                var logChan = _client.GetChannel(Setup.DiscordLogChannel) as SocketTextChannel;

                string message = $"{time} [VOICE] {user.Username}#{user.Discriminator} has CONNECTED to " +
                    $"{voiceAfter.VoiceChannel.Name}. " +
                    $"\n({user.Id})";

                await logChan.SendMessageAsync($"```{message}```");
                Console.WriteLine($"{time} Discord     {message}");
            }

            //Changed Voice Channel
            else if (voiceBefore.VoiceChannel != null && voiceAfter.VoiceChannel != null && (voiceBefore.VoiceChannel.Id != voiceAfter.VoiceChannel.Id))
            {
                var time = DateTime.Now.ToString("HH:MM:ss");
                var logChan = _client.GetChannel(Setup.DiscordLogChannel) as SocketTextChannel;

                string message = $"{time} [VOICE] {user.Username}#{user.Discriminator} has CHANGED from " +
                    $"{voiceBefore.VoiceChannel.Name} to {voiceAfter.VoiceChannel.Name}. " +
                    $"\n({user.Id})";

                await logChan.SendMessageAsync($"```{message}```");
                Console.WriteLine($"{time} Discord     {message}");
            }

            //Left Voice Channel
            else if (voiceBefore.VoiceChannel != null && voiceAfter.VoiceChannel == null)
            {
                var time = DateTime.Now.ToString("HH:MM:ss");
                var logChan = _client.GetChannel(Setup.DiscordLogChannel) as SocketTextChannel;

                string message = $"{time} [VOICE] {user.Username}#{user.Discriminator} has DISCONNECTED. " +
                    $"\n({user.Id})";

                await logChan.SendMessageAsync($"```{message}```");
                Console.WriteLine($"{time} Discord     {message}");
            }
        }

        private async Task Client_UserUnbanned(SocketUser user, SocketGuild guild)
        {
            if (Setup.DiscordLogChannel == 0) return;

            var time = DateTime.Now.ToString("HH:MM:ss");
            var logChan = _client.GetChannel(Setup.DiscordLogChannel) as SocketTextChannel;

            string message = $"{time} [SERVER] {user.Username}#{user.Discriminator} has been UNBANNED. " +
                $"\n({user.Id})";

            await logChan.SendMessageAsync($"```{message}```");
            Console.WriteLine($"{time} Discord     {message}");
        }

        private async Task Client_UserBanned(SocketUser user, SocketGuild guild)
        {
            if (Setup.DiscordLogChannel == 0) return;

            var time = DateTime.Now.ToString("HH:MM:ss");
            var logChan = _client.GetChannel(Setup.DiscordLogChannel) as SocketTextChannel;

            string message = $"{time} [SERVER] {user.Username}#{user.Discriminator} has been BANNED. " +
                $"\n({user.Id})";

            await logChan.SendMessageAsync($"```{message}```");
            Console.WriteLine($"{time} Discord     {message}");
        }

        private async Task Client_UserLeft(SocketGuildUser guildUser)
        {
            if (Setup.DiscordLogChannel == 0) return;

            var logChan = _client.GetChannel(Setup.DiscordLogChannel) as SocketTextChannel;
            var time = DateTime.Now.ToString("HH:MM:ss");

            string message = $"{time} [SERVER] {guildUser.Username}#{guildUser.Discriminator} has LEFT or KICKED. " +
                $"\n({guildUser.Id})";

            await logChan.SendMessageAsync($"```{message}```");
            Console.WriteLine($"{time} Discord     {message}");
        }

        private async Task Client_UserJoined(SocketGuildUser guildUser)
        {
            if (Setup.DiscordLogChannel == 0) return;

            var logChan = _client.GetChannel(Setup.DiscordLogChannel) as SocketTextChannel;
            var time = DateTime.Now.ToString("HH:MM:ss");

            string message = $"{time} [SERVER] {guildUser.Username}#{guildUser.Discriminator} has JOINED. " +
                $"\n({guildUser.Id})";

            await logChan.SendMessageAsync($"```{message}```");
            Console.WriteLine($"{time} Discord     {message}");
        }
    }
}
