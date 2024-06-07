using BluBotCore.Handlers.Discord;
using BluBotCore.Services;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TwitchLib.EventSub.Websockets.Extensions;

namespace BluBotCore
{
    class Program
    {
        public static void Main(string[] args)
        {

            CreateHostBuilder(args)//.Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<Startup>();

                    services.AddLogging();
                    services.AddTwitchLibEventSubWebsockets();

                    services.AddHostedService<EventSubHostedService>();
                    services.AddHostedService<LiveMonitor>();

                    services.AddHostedService<DiscordClient>();


                    //.AddSingleton(_client)
                    //.AddSingleton(new CommandService(new CommandServiceConfig
                    //{
                    //    ThrowOnError = true,
                    //    CaseSensitiveCommands = false,
                    //    DefaultRunMode = RunMode.Async
                    //}))
                    //.AddSingleton<CommandHandler>()
                    //.AddSingleton<LogHandler>()
                    //.AddSingleton<ClientHandler>()
                    //.AddSingleton<LiveMonitor>()
                    ////.AddSingleton<EventSubHostedService>()
                    //.BuildServiceProvider();
                });
    }
}