using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BluBotCore.Modules.Commands
{
    [Name("Public")]
    public class PublicCmds : ModuleBase<SocketCommandContext>
    {
        private readonly Random rnd = new();

        [Command("hug")]
        [Summary("Random Hug image or Hug a target.")]
        public async Task HugAsync(IUser target = null)
        {
        string result = "";
        if (target != null) result = $"{Context.User.Username} hugs {target.Username} ";
            List<string> hug = new()
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

        [Command("cflip")]
        [Alias("coinflip")]
        [Summary("Flip a coin.")]
        public async Task CoinFlipAsync()
        {
            string choice;
            int r = rnd.Next(2);
            if (r == 0) choice = "Heads";
            else choice = "Tails";
            await ReplyAsync($"Coin shows `{choice}`");
        }
    }
}
