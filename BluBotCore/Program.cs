using BluBotCore.Services;
using System.Threading.Tasks;

namespace BluBotCore
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            DiscordClient _discord = new DiscordClient();
            await _discord.MainAsync();
        }
    }
}
