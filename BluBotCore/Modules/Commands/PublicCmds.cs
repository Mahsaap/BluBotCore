using BluBotCore.Enums;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;


namespace BluBotCore.Modules.Commands
{
    [Name("Public")]
    public class PublicCmds : ModuleBase<SocketCommandContext>
    {
        private readonly Random rnd = new Random();

        [Command("hug")]
        [Summary("Random Hug image or Hug a target.")]
        public async Task HugAsync(IUser target = null)
        {
        string result = "";
        if (target != null) result = $"{Context.User.Username} hugs {target.Username} ";
            List<string> hug = new List<string>()
            {
            "༼ つ ◕_◕ ༽つ",
            "<(°^°<0",
            "(っ◕‿◕)っ",
            "<(O^O<)",
            "(>'-')>"
            };
            int r = rnd.Next(hug.Count);
            await ReplyAsync(result + hug[r]);
        }

        [Command("report")]
        [Alias("reporting","reports")]
        [Summary("Discord link with instructions on reporting a user.")]
        public async Task ReportAsync()
        {
            await ReplyAsync("https://support.discordapp.com/hc/en-us/articles/360000291932-How-to-Properly-Report-Issues-to-Trust-Safety");
        }

        [Command("cflip")]
        [Alias("coinflip")]
        [Summary("Flip a coin.")]
        public async Task CoinFlipAsync()
        {
            Coin choice;
            int r = rnd.Next(2);
            if (r == 0) choice = Coin.Heads;
            else choice = Coin.Tails;
            await ReplyAsync($"Coin shows `{choice}`");
        }

        [Command("status")]
        [Summary("Links for server/API status for Discord/Steam/Twitch.")]
        public async Task StatusAsync()
        {
            await ReplyAsync("**Discord - Steam - Twitch Server Status Links:**\n" +
                @"<https://status.discordapp.com/>" + "\n" +
                @"<https://steamstat.us/>" + "\n" +
                @"<https://twitchstatus.com/>");
        }
    }
}
