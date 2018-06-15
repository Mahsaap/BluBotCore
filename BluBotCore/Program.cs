using BluBotCore.Services;

namespace BluBotCore
{
    class Program
    {
        public static void Main(string[] args)
            => new DiscordClient().MainAsync().GetAwaiter().GetResult();
    }
}
