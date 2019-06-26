using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
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
            LoadCustomCmdFile();
            await _client.SetStatusAsync(UserStatus.Online);
            await _client.SetGameAsync("WYKTV Monitoring");

        }

        private void LoadCustomCmdFile()
        {
            string filename = "customcmds.txt";
            if (File.Exists(filename))
            {
                ConcurrentDictionary<string, string> tmpList = new ConcurrentDictionary<string, string>();
                string dataNew;
                Console.WriteLine($"{Global.CurrentTime} Setup       File {filename} exists!");
                using (StreamReader file = new StreamReader(filename))
                {
                    while ((dataNew = file.ReadLine()) != null)
                    {
                        var split = dataNew.Split('~');
                        if (split.Length <= 1) return;
                        tmpList.TryAdd(split[0], split[1]);
                    }
                    file.Close();
                }
                CustomCommandsHandler.customCommands = tmpList;
                Console.WriteLine($"{Global.CurrentTime} Setup       File {filename} loaded!");
            }
            else
            {
                Console.WriteLine($"{Global.CurrentTime} Setup       File {filename} does not exist!");
                using (StreamWriter file = new StreamWriter(filename, true, Encoding.UTF8))
                {
                    file.Flush();
                    file.Close();
                }
                Console.WriteLine($"{Global.CurrentTime} Setup       File {filename} has been created!");
            }
        }
    }
}
