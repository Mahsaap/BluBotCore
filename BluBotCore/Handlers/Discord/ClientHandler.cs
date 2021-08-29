using BluBotCore.Global;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace BluBotCore.Handlers.Discord
{
    public class ClientHandler
    {
        private readonly DiscordSocketClient _client;
        public ClientHandler(DiscordSocketClient client)
        {
            _client = client;

            _client.Ready += Client_Ready;
        }

        private async Task Client_Ready()
        {
            await _client.SetStatusAsync(UserStatus.Online);
            if (Version.Build == BuildType.WYK.Value)
            {
                await _client.SetGameAsync("WYKTV Monitoring");
            }
            else if (Version.Build == BuildType.OBG.Value)
            {
                await _client.SetGameAsync("is OBG Live?");
            }
        }
    }
}
