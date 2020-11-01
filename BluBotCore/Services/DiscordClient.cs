using BluBotCore.Handlers.Discord;
using BluBotCore.Other;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.Net.Providers.WS4Net;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

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
                WebSocketProvider = WS4NetProvider.Instance,
                LogLevel = LogSeverity.Debug,
                MessageCacheSize = 100,
                AlwaysDownloadUsers = true,
                ExclusiveBulkDelete = true
            });

            IServiceProvider service = ConfigServices();
            await GetRequiredServicesAsync(service);

            await _client.LoginAsync(TokenType.Bot, AES.Decrypt(Cred.DiscordTok));
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private void CheckSetupFile()
        {

            string filename = "setup.txt";
            if (!File.Exists(filename))
            {
                using (StreamWriter file = new StreamWriter(filename, true, Encoding.UTF8))
                {
                    file.WriteLine(Setup.DiscordAnnounceChannel);
                    file.WriteLine(Setup.DiscordStaffRole);
                    file.WriteLine(Setup.DiscordWYKTVRole);
                    file.WriteLine(Setup.DiscordLogChannel);
                    file.Flush();
                    file.Close();
                }
                Console.WriteLine($"{Globals.CurrentTime} Setup       File {filename} created!");

            }
            List<string> tmpList = new List<string>();
            string data;
            using (StreamReader file = new StreamReader(filename))
            {
                while ((data = file.ReadLine()) != null)
                    tmpList.Add(data);
                file.Close();
            }
            Setup.DiscordAnnounceChannel = Convert.ToUInt64(tmpList[0]);
            Setup.DiscordStaffRole = Convert.ToUInt64(tmpList[1]);
            Setup.DiscordWYKTVRole = Convert.ToUInt64(tmpList[2]);
            //Seemless change over with additional channel entry - Can be removed after used once.
            if (tmpList.Count == 3)
            {
                tmpList.Add("0");
                using StreamWriter file = new StreamWriter(filename, true, Encoding.UTF8);
                file.WriteLine("0");
                file.Flush();
                file.Close();
            }
            Setup.DiscordLogChannel = Convert.ToUInt64(tmpList[3]);
            Console.WriteLine($"{Globals.CurrentTime} Setup       File {filename} loaded!");
        }

        private bool CheckInitFile()
        {
            string filename = "init.txt";
            if (File.Exists(filename))
            {
                string data;
                List<string> tmpList = new List<string>();
                using (StreamReader file = new StreamReader(filename))
                {
                    while ((data = file.ReadLine()) != null)
                        tmpList.Add(data);
                    file.Close();
                }
                Cred.DiscordTok = tmpList[0];
                Cred.TwitchAPIID = tmpList[1];
                Cred.TwitchAPISecret = tmpList[2];
                Cred.TwitchAPIRefreshToken = tmpList[3];
                Console.WriteLine($"{Globals.CurrentTime} Setup       File {filename} loaded!");
                return true;
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

                string data0 = AES.Encrypt(discordToken);
                string data1 = AES.Encrypt(twitchapiID);
                string data2 = AES.Encrypt(twitchapiSecret);
                string data3 = AES.Encrypt(twitchapiRefreshToken);

                using (StreamWriter file = new StreamWriter(filename, true, Encoding.UTF8))
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

        private IServiceProvider ConfigServices()
        {
            return new ServiceCollection()
            .AddSingleton(_client)
            .AddSingleton(new CommandService(new CommandServiceConfig
            {
                ThrowOnError = true,
                CaseSensitiveCommands = false,
                //DefaultRunMode = RunMode.Async
            }))
            .AddSingleton<CommandHandler>()
            .AddSingleton<InteractiveService>()
            .AddSingleton<LogHandler>()
            .AddSingleton<ClientHandler>()
            .AddSingleton<UserHandler>()
            .AddSingleton<CustomCommandsHandler>()
            .AddSingleton<LiveMonitor>()
            .BuildServiceProvider();
        }

        private async Task GetRequiredServicesAsync(IServiceProvider service)
        {
            await service.GetRequiredService<CommandHandler>().InstallCommandsAsync();
            service.GetRequiredService<LogHandler>();
            service.GetRequiredService<ClientHandler>();
            service.GetRequiredService<UserHandler>();
            service.GetRequiredService<CustomCommandsHandler>();
            service.GetRequiredService<LiveMonitor>();
        }
    }
}
