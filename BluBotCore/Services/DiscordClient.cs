using BluBotCore.Handlers.Discord;
using BluBotCore.Other;
using Discord;
using Discord.Commands;
using Discord.Net.Providers.WS4Net;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using TwitchLib.EventSub.Core;
using TwitchLib.EventSub.Webhooks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.EventSub.Webhooks.Extensions;

namespace BluBotCore.Services
{
    class DiscordClient
    {
        public DiscordSocketClient _client;

        public async Task MainAsync()
        {
            bool rdy = CheckInitFile();
            CheckSetupFile();
            if (!rdy) Console.ReadLine();
            else await StartAsync();
        }

        public async Task StartAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.All,
                WebSocketProvider = WS4NetProvider.Instance,
                LogLevel = LogSeverity.Info,
                MessageCacheSize = 100,
                AlwaysDownloadUsers = true,
            });

            IServiceProvider service = ConfigServices();
            await GetRequiredServicesAsync(service);

            await _client.LoginAsync(TokenType.Bot, Cred.DiscordTok);
            await _client.StartAsync();
            await Task.Delay(-1);
        }

        private static bool CheckInitFile()
        {
            string filename = "init.txt";
            if (File.Exists(filename))
            {
                string data;
                List<string> tmpList = new();
                using (StreamReader file = new(filename))
                {
                    while ((data = file.ReadLine()) != null)
                        tmpList.Add(data);
                    file.Close();
                }
                if (tmpList.Count <= 4)
                {
                    Cred.DiscordTok = tmpList[0];
                    Cred.TwitchAPIID = tmpList[1];
                    Cred.TwitchAPISecret = tmpList[2];
                    Cred.TwitchAPIRefreshToken = tmpList[3];
                    Console.WriteLine($"{Globals.CurrentTime} Setup       File {filename} loaded!");
                    return true;
                }
                else
                {
                    Console.WriteLine($"{Globals.CurrentTime} Setup       File {filename} has {tmpList.Count} entries and should be 4!" +
                        $"\n                     Please check {filename} and reboot the bot.");
                    return false;
                }
            }
            else
            {
                Console.WriteLine($"{Globals.CurrentTime} Setup       No {filename} file found!");
                Console.WriteLine($"{Globals.CurrentTime} Setup       Please enter your DISCORD TOKEN and press return.");
                string discordToken = Console.ReadLine();
                Console.Clear();
                Console.WriteLine($"{Globals.CurrentTime} Setup       Please enter your TWITCHAPI ID and press return.");
                string twitchapiID = Console.ReadLine();
                Console.Clear();
                Console.WriteLine($"{Globals.CurrentTime} Setup       Please enter your TWITCHAPI SECRET and press return.");
                string twitchapiSecret = Console.ReadLine();
                Console.Clear();
                Console.WriteLine($"{Globals.CurrentTime} Setup       Please enter your TWITCHAPI REFRESH TOKEN and press return.");
                string twitchapiRefreshToken = Console.ReadLine();
                Console.Clear();

                string data0 = discordToken;
                string data1 = twitchapiID;
                string data2 = twitchapiSecret;
                string data3 = twitchapiRefreshToken;

                using (StreamWriter file = new(filename, true, Encoding.UTF8))
                {
                    file.WriteLine(data0);
                    file.WriteLine(data1);
                    file.WriteLine(data2);
                    file.WriteLine(data3);
                    file.Flush();
                    file.Close();
                }
                Console.WriteLine($"{Globals.CurrentTime} Setup       File {filename} has been generated.");
                Console.WriteLine($"{Globals.CurrentTime} Setup       Please restart the program.");
                return false;
            }
        }

        private static void CheckSetupFile()
        {
            string filename = "setup.txt";
            if (!File.Exists(filename))
            {
                using (StreamWriter file = new(filename, true, Encoding.UTF8))
                {
                    file.WriteLine(Setup.DiscordAnnounceChannel);
                    file.WriteLine(Setup.DiscordStaffRole);
                    file.WriteLine(Setup.DiscordWYKTVRole);
                    file.Flush();
                    file.Close();
                }
                Console.WriteLine($"{Globals.CurrentTime} Setup       File {filename} created!");
            }
            List<string> tmpList = new();
            string data;
            using (StreamReader file = new(filename))
            {
                while ((data = file.ReadLine()) != null)
                    tmpList.Add(data);
                file.Close();
            }
            Setup.DiscordAnnounceChannel = Convert.ToUInt64(tmpList[0]);
            Setup.DiscordStaffRole = Convert.ToUInt64(tmpList[1]);
            Setup.DiscordWYKTVRole = Convert.ToUInt64(tmpList[2]);
            Console.WriteLine($"{Globals.CurrentTime} Setup       File {filename} loaded!");
        }

        private IServiceProvider ConfigServices()
        {
            return new ServiceCollection()
            .AddSingleton(_client)
            .AddSingleton(new CommandService(new CommandServiceConfig
            {
                ThrowOnError = true,
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Async
            }))
            .AddSingleton<CommandHandler>()
            .AddSingleton<LogHandler>()
            .AddSingleton<ClientHandler>()
            .AddSingleton<LiveMonitor>()
            //.AddSingleton<EventSubHostedService>()
            .BuildServiceProvider();
        }

        private async Task GetRequiredServicesAsync(IServiceProvider service)
        {
            await service.GetRequiredService<CommandHandler>().InstallCommandsAsync();
            service.GetRequiredService<LogHandler>();
            service.GetRequiredService<ClientHandler>();
            service.GetRequiredService<LiveMonitor>();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddTwitchLibEventSubWebhooks(config =>
            {
                config.CallbackPath = "/webhooks";
                config.Secret = "supersecuresecret";
                config.EnableLogging = true;
            });

            services.AddHostedService<EventSubHostedService>();
        }
    }
}