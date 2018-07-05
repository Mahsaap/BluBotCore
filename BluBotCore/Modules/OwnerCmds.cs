﻿using BluBotCore.DiscordHandlers;
using BluBotCore.Other;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BluBotCore.Modules
{
    [Name("Owner")]
    [RequireOwner]
    public class OwnerCmds : ModuleBase<SocketCommandContext>
    {
        [Command("GetRoles")]
        public async Task GetRoleIDsAsync()
        {
            var roles = Context.Guild.Roles;
            string result = $"Current roles in {Context.Guild.Name}:\n\n";
            foreach (SocketRole role in roles)
            {
                result += $"{role.Name} - ({role.Id})\n";
            }
            await ReplyAsync(result);
        }

        [Command("SetStaffRole")]
        public async Task StaffRoleSet(IRole role)
        {
            Setup.DiscordStaffRole = role.Id;
            await ReplyAsync($"Staff/Admin role set as {role.Name} - ({role.Id})");
            string filename = "setup.txt";
            List<string> tempLst = new List<string>()
            {
                Setup.DiscordAnnounceChannel.ToString(),
                Setup.DiscordStaffRole.ToString(),
                Setup.DiscordWYKTVRole.ToString()
            };
            File.WriteAllLines(filename, tempLst);
        }

        [Command("egg")]
        public async Task EggAsync()
        {
            EasterEggs.easterEggEnable = !EasterEggs.easterEggEnable;
            await Task.CompletedTask;
        }

        [Command("encrypt")]
        [RequireContext(ContextType.DM)]
        public async Task VerifyAsync(string entry)
        {
            string result = AES.Encrypt(entry);
            await ReplyAsync(result);
        }

        [Command("decrypt")]
        [RequireContext(ContextType.DM)]
        public async Task VerifyDecryptAsync(string entry)
        {
            string result = AES.Decrypt(entry);
            await ReplyAsync(result);
        }
    }
}
