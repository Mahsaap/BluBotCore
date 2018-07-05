using BluBotCore.Other;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BluBotCore.DiscordHandlers
{
    class CustomCommands
    {
        public static ConcurrentDictionary<string, string> customCommands = new ConcurrentDictionary<string, string>();
        public static string customCMDPrefix = "?";

        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _service;

        public CustomCommands(IServiceProvider service, DiscordSocketClient client)
        {
            _client = client;
            _service = service;

            _client.MessageReceived += Client_MessageReceived;
        }

        private async Task Client_MessageReceived(SocketMessage message)
        {
            if (message.Author.Id == _client.CurrentUser.Id) return;
            var chan = message.Channel as SocketTextChannel;

            #region Custom Commands
            if (message.Content.StartsWith(customCMDPrefix))
            {
                string str = message.Content.Substring(customCMDPrefix.Length);
                string[] results = str.Split(' ', 2);
                string command = results[0].ToLower();
                string value = "";
                if (results.Length > 1)
                {
                    value = results[1];
                }

                if ((message.Author as IGuildUser).RoleIds.Contains(Setup.DiscordStaffRole) ||
                (message.Author as IGuildUser).RoleIds.Contains(Setup.DiscordWYKTVRole) || (message.Author.Id == Constants.Discord.Mahsaap))
                {
                    //Add Check
                    if (command == "add")
                    {
                        string[] value2 = value.Split(' ', 2);
                        string cmdAdd = value2[0].ToLower();
                        if (String.IsNullOrEmpty(value2[1])) return;
                        string valueAdd = value2[1];

                        if (!customCommands.ContainsKey(cmdAdd))
                        {
                            // add checks for  opther commands in this method
                            customCommands.TryAdd(cmdAdd, valueAdd);
                            await chan.SendMessageAsync($"Tag `{cmdAdd}` has been added with a value of `{valueAdd}`.");

                            string filename = "customcmds.txt";
                            using (StreamWriter file = new StreamWriter(filename, true, Encoding.UTF8))
                            {
                                file.WriteLine($"{cmdAdd}~{valueAdd}");
                                file.Flush();
                                file.Close();
                            }
                        }
                        else
                        {
                            await chan.SendMessageAsync($"There already is a tag named `{cmdAdd}`.");
                        }
                    }
                    //Remove check
                    else if (command == "remove")
                    {
                        string[] value2 = value.Split(' ', 2);
                        string cmdRemove = value2[0].ToLower();
                        //string valueRemove = value2[1];
                        if (customCommands.ContainsKey(cmdRemove))
                        {
                            customCommands.TryRemove(cmdRemove, out string removeArg);
                            await chan.SendMessageAsync($"Tag `{cmdRemove}` has been removed.");

                            string filename = "customcmds.txt";
                            List<string> tmpList = new List<string>();
                            foreach(var cmd in customCommands)
                            {
                                tmpList.Add($"{cmd.Key}~{cmd.Value}");
                            }
                            File.WriteAllLines(filename, tmpList);
                        }
                        else
                        {
                            await chan.SendMessageAsync($"There is no tag named `{cmdRemove}`.");
                        }
                    }
                    //edit check
                    else if (command == "edit")
                    {
                        string[] value2 = value.Split(' ', 2);
                        string cmdEdit = value2[0].ToLower();
                        if (String.IsNullOrEmpty(value2[1])) return;
                        string valueEdit = value2[1];
                        if (customCommands.ContainsKey(cmdEdit))
                        {
                            string result = $"Tag `{cmdEdit}` has been changed from a value of `{customCommands[cmdEdit]}` to `{valueEdit}`.";
                            customCommands[cmdEdit] = valueEdit;
                            await chan.SendMessageAsync(result);

                            string filename = "customcmds.txt";
                            List<string> tmpList = new List<string>();
                            foreach (var cmd in customCommands)
                            {
                                tmpList.Add($"{cmd.Key}~{cmd.Value}");
                            }
                            File.WriteAllLines(filename, tmpList);
                        }
                        else
                        {
                            await chan.SendMessageAsync($"There is no tag named `{cmdEdit}`.");
                        }
                    }
                }
                if (command == "list")
                {
                    string result = "**tags:**\n";
                    foreach (string key in customCommands.Keys)
                    {
                        result += $"{key}, ";
                    }
                    await chan.SendMessageAsync(result.Remove(result.Length - 2, 2));

                }
                else if (!customCommands.ContainsKey(command)) return;
                else if (customCommands.ContainsKey(command.ToLower()))
                {
                    await chan.SendMessageAsync(customCommands[command]);
                }
            }
            #endregion


            /*
             *
             * Notes: On custom commands
             *
             * Adding > check for modules do not contain in both command and alias
             *  > check the dictionary doesnt already have that command.
             *  > Only admin can add / remove / edit
             * removing > if the dictionary contains it
             * edit commands > ensure its htere in the dictionary
             *
             * save Dictionary to file. - do last after setup and working.
             *
             */


        }
    }
}
