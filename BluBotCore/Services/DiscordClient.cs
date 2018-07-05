using BluBotCore.DiscordHandlers;
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
        private DiscordSocketClient _client;

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
                LogLevel = LogSeverity.Info,
                MessageCacheSize = 100,
                AlwaysDownloadUsers = true
            });

            IServiceProvider service = ConfigServices();
            await GetRequiredServicesAsync(service);

            await _client.LoginAsync(TokenType.Bot, AES.Decrypt(Cred.DiscordTok));
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private void CheckSetupFile()
        {
            string time = DateTime.Now.ToString("HH:MM:ss");
            string filename = "setup.txt";
            if (!File.Exists(filename))
            {
                Console.WriteLine($"{time} Setup       File {filename} not found!");
                using (StreamWriter file = new StreamWriter(filename, true, Encoding.UTF8))
                {
                    file.WriteLine(Setup.DiscordAnnounceChannel);
                    file.WriteLine(Setup.DiscordStaffRole);
                    file.WriteLine(Setup.DiscordWYKTVRole);
                    file.WriteLine(Setup.DiscordLogChannel);
                    file.Flush();
                    file.Close();
                }
                Console.WriteLine($"{time} Setup       File {filename} created!");

            }
            Console.WriteLine($"{time} Setup       File {filename} found!");
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
                using (StreamWriter file = new StreamWriter(filename, true, Encoding.UTF8))
                {
                    file.WriteLine("0");
                    file.Flush();
                    file.Close();
                }
            }
            Setup.DiscordLogChannel = Convert.ToUInt64(tmpList[3]);
            Console.WriteLine($"{time} Setup       File {filename} loaded!");
        }

        private bool CheckInitFile()
        {
            string time = DateTime.Now.ToString("HH:MM:ss");
            string filename = "init.txt";
            if (File.Exists(filename))
            {
                string data;
                List<string> tmpList = new List<string>();
                Console.WriteLine($"{time} Setup       File {filename} exists!");
                using (StreamReader file = new StreamReader(filename))
                {
                    while ((data = file.ReadLine()) != null)
                        tmpList.Add(data);
                    file.Close();
                }
                Cred.DiscordTok = tmpList[0];
                Cred.TwitchAPIID = tmpList[1];
                Cred.TwitchAPIToken = tmpList[2];
                Cred.TwitchAPIRefreshToken = tmpList[3];
                Console.WriteLine($"{time} Setup       File {filename} loaded!");
                CheckEntryFile();
                if (tmpList.Count >= 8)
                {
                    Cred.TwitterConsumerKey = tmpList[4];
                    Cred.TwitterConsumerSecret = tmpList[5];
                    Cred.TwitterAccessKey = tmpList[6];
                    Cred.TwitterAccessSecret = tmpList[7];
                    Console.WriteLine($"{time} Setup       Twitter Credentials Loaded");
                    return true;
                }
                else
                {
                    Console.WriteLine($"{time} Setup       File entry.txt not found and twitter information does not seem to be present!");
                    Console.WriteLine($"{time} Setup       Please add your twitter keys in entry.txt and restart the program.");
                    return false;
                }
            }
            else
            {
                Console.WriteLine($"{time} Setup       No {filename} file found!");
                Console.WriteLine($"{time} Setup       Please enter your DISCORD TOKEN and press return.");
                string discordToken = Console.ReadLine();
                Console.Clear();
                Console.WriteLine($"{time} Setup       Please enter your TWITCHAPI ID and press return.");
                string twitchapiID = Console.ReadLine();
                Console.Clear();
                Console.WriteLine($"{time} Setup       Please enter your TWITCHAPI TOKEN and press return.");
                string twitchapiToken = Console.ReadLine();
                Console.Clear();
                Console.WriteLine($"{time} Setup       Please enter your TWITCHAPI REFRESH TOKEN and press return.");
                string twitchapiRefreshToken = Console.ReadLine();
                Console.Clear();

                string data0 = AES.Encrypt(discordToken);
                string data1 = AES.Encrypt(twitchapiID);
                string data2 = AES.Encrypt(twitchapiToken);
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
                Console.WriteLine($"{time} Setup       File {filename} has been generated.");
                Console.WriteLine($"{time} Setup       Please restart the program.");
                return false;
            }
        }

        private void CheckEntryFile()
        {
            string time = DateTime.Now.ToString("HH:MM:ss");
            string filename = "entry.txt";
            if (File.Exists(filename) && File.Exists("init.txt"))
            {
                string dataOld;
                List<string> tmpList = new List<string>();
                using (StreamReader file = new StreamReader("init.txt"))
                {
                    while ((dataOld = file.ReadLine()) != null)
                        tmpList.Add(dataOld);
                    file.Close();
                }
                string dataNew;
                Console.WriteLine($"{time} Setup       File {filename} exists!");
                using (StreamReader file = new StreamReader(filename))
                {
                    while ((dataNew = file.ReadLine()) != null)
                        tmpList.Add(AES.Encrypt(dataNew));
                    file.Close();
                }
                Cred.TwitterConsumerKey = tmpList[4];
                Cred.TwitterConsumerSecret = tmpList[5];
                Cred.TwitterAccessKey = tmpList[6];
                Cred.TwitterAccessSecret = tmpList[7];
                Console.WriteLine($"{time} Setup       File {filename} loaded!");
                //Rewrites file from list<string>
                File.WriteAllLines("init.txt", tmpList);
                try
                {
                    File.Delete(filename);
                    Console.WriteLine($"{time} Setup       File {filename} removed! (Data is now ecrypted and Stored in init.txt)");
                }
                catch
                {
                    Console.WriteLine("WARNING! VERIFY THIS FILE HAS BEEN REMOVED <> IF NOT PLEASE DELETE FILE, IT HAS BEEN LOADED!");
                }
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
            .AddSingleton<CustomCommands>()
            .AddSingleton<EasterEggs>()
            .AddSingleton<TwitterClient>()
            .AddSingleton<LiveMonitor>()
            .BuildServiceProvider();
        }

        private async Task GetRequiredServicesAsync(IServiceProvider service)
        {
            await service.GetRequiredService<CommandHandler>().InstallCommandsAsync();
            service.GetRequiredService<LogHandler>();
            service.GetRequiredService<ClientHandler>();
            service.GetRequiredService<UserHandler>();
            service.GetRequiredService<CustomCommands>();
            service.GetRequiredService<EasterEggs>();
            service.GetRequiredService<TwitterClient>();
            service.GetRequiredService<LiveMonitor>();
        }
    }
}
