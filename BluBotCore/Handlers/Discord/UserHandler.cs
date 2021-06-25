using BluBotCore.Other;
using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace BluBotCore.Handlers.Discord
{
    class UserHandler
    {
        private readonly DiscordSocketClient _client;

        public UserHandler(DiscordSocketClient client)
        {
            _client = client;

            _client.UserJoined += Client_UserJoined;
            _client.UserLeft += Client_UserLeft;
            _client.UserBanned += Client_UserBanned;
            _client.UserUnbanned += Client_UserUnbanned;
        }

        private static string CreateMessage(string username, string discriminator, string id, string type)
        {
            return $"{Globals.CurrentTime} [SERVER] {username}#{discriminator} has been {type}. " +
                $"\n({id})";
        }

        private static void ConsoleWrite(string message)
        {
            Console.WriteLine($"{Globals.CurrentTime} Discord     {message}");
        }

        private async Task SendDiscordAsync(string message)
        {
            if (_client.ConnectionState == ConnectionState.Connected) return;
            if (Setup.DiscordLogChannel == 0) return;
            var logChan = _client.GetChannel(Setup.DiscordLogChannel) as SocketTextChannel;
            await logChan.SendMessageAsync($"```{message}```");
        }

        private async Task Client_UserUnbanned(SocketUser user, SocketGuild guild)
        {
            string message = CreateMessage(user.Username, user.Discriminator, user.Id.ToString(), "UNBANNED");
            ConsoleWrite(message);
            await SendDiscordAsync(message);
        }

        private async Task Client_UserBanned(SocketUser user, SocketGuild guild)
        {
            string message = CreateMessage(user.Username, user.Discriminator, user.Id.ToString(), "BANNED");
            ConsoleWrite(message);
            await SendDiscordAsync(message);
        }

        private async Task Client_UserLeft(SocketGuildUser guildUser)
        {
            string message = CreateMessage(guildUser.Username, guildUser.Discriminator, guildUser.Id.ToString(), "LEFT OR KICKED");
            ConsoleWrite(message);
            await SendDiscordAsync(message);
        }

        private async Task Client_UserJoined(SocketGuildUser guildUser)
        {
            string message = CreateMessage(guildUser.Username, guildUser.Discriminator, guildUser.Id.ToString(), "JOINED");
            ConsoleWrite(message);
            await SendDiscordAsync(message);
        }
    }
}
