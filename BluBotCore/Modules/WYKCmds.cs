﻿using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using System.Linq;
using BluBotCore.Other;
using Discord.WebSocket;

namespace BluBotCore.Modules
{
    [Name("WYK")]
    [Group("wyk")]
    public class WYKCmds : ModuleBase<SocketCommandContext>
    {
        [Command("social")]
        [Summary("WYKTV Website/FB/Twitter/Youtube links.")]
        public async Task WYKAsync()
        {
            await ReplyAsync("**Website =** <http://www.wouldyoukindly.com/> \n" +
                "**Facebook =** <https://www.facebook.com/wouldyoukindlydotcom> \n" +
                "**Twitter =** <https://twitter.com/wouldyoukindly> \n" +
                "**YouTube =** <https://www.youtube.com/channel/UCDfmQq5QEUeg7fjfRADcsvw>");
        }

        [Command("staff")]
        [Summary("WYKTV staff list.")]
        public async Task WYKStaffAsync()
        {
            string result = "**Would You Kindly - Staff**\n";
            var staff = Context.Guild.GetRole(Setup.DiscordStaffRole).Members;
            foreach (SocketGuildUser user in staff)
            {
                result += $"*{user.Username}*\n";
            }
            await ReplyAsync(result);
        }

        [Command("owner")]
        [Summary("Discord WYKTV server owner.")]
        public async Task WYKOwner()
        {
            string result = "**Would You Kindly - Owner**\n";
            foreach (SocketGuildUser user in Context.Guild.Users)
            {
                if (user.Id == Context.Guild.OwnerId) result += $"*{user.Username}*\n";
            }
            await ReplyAsync(result);
        }

        [Command("team")]
        [Alias("members", "casts","casters", "streamers", "streams")]
        [Summary("WYKTV Caster/Team members.")]
        public async Task WYKStreamers()
        {
            string result = "**Would You Kindly - Team**\n";
            var staff = Context.Guild.GetRole(Setup.DiscordWYKTVRole).Members;
            foreach (SocketGuildUser user in staff)
            {
                result += $"*{user.Username}*\n";
            }
            await ReplyAsync(result);
        }
    }

}
