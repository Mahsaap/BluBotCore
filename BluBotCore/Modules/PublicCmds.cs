using BluBotCore.Enums;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;


namespace BluBotCore.Modules
{
    [Name("Public")]
    public class PublicCmds : ModuleBase<SocketCommandContext>
    {
        private Random rnd = new Random();

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

        //[Command("choose"), Summary("Choose Random from given")]
        //[Alias("pick")]
        //public async Task ChooseAsync(string entry)
        //{
        //    List<string> entries = entry.Split(',').ToList();
        //    int r = rnd.Next(entries.Count);
        //    await ReplyAsync($"I choose `{entries[r].Trim()}`");
        //}

        //[Command("weather")]
        //public async Task WeatherAsync([Remainder]string input)
        //{
        //    var entries = input.Split(',');
        //    await Task.CompletedTask;
        //}

        [Command("cflip")]
        [Alias("coinflip")]
        [Summary("Flip a coin.")]
        public async Task CoinFlipAsync()
        {
            Coin choice = 0;
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

        private static string GetUptime()
            => (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss");
        }

}
