using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using SteamStoreQuery;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BluBotCore.Modules.Commands
{
    [Name("Steam")]
    public class SteamCmds : InteractiveBase<SocketCommandContext>
    {
        [Command("steam", RunMode = RunMode.Async)]
        [Summary("Search the steam store (!steam game).")]
        public async Task SteamQueryAsync([Remainder]string game)
        {
            game = game.Replace(' ', '_');
            try
            {
                List<Listing> results = Query.Search(game);
                string games = $"Please select a game from the list below:```";
                for (int i = 0; i < results.Count; i++)
                {
                    games += $"{i + 1} - {results[i].Name}\n";
                }
                games += $"```\n*Search will timeout after 20 seconds or type EXIT to cancel.*";
                if (results.Count != 0 && results.Count != 1)
                {
                    IUserMessage resultList = await ReplyAsync(games);
                    SocketMessage response = await NextMessageAsync(timeout: TimeSpan.FromSeconds(20));
                    if (response.Content.ToLower().Contains("exit"))
                    {
                        await ReplyAsync("Search Cancelled!");
                        return;
                    }
                    bool works = int.TryParse(response.Content, out int x);
                    if (works && x > 0 && x <= results.Count)
                    {
                        await ReplyAsync($"**{results[x - 1].Name}**\n" +
                            $"{results[x - 1].PriceUSD} USD\n" +
                            $"{results[x - 1].StoreLink}");
                    }
                    else if (works && (x <= 0 || x > results.Count))
                    {
                        await ReplyAsync($"That number was not in the list, please try your search again.");
                    }
                    else
                    {
                        await ReplyAsync("That is not a valid number, please try your search again.");
                    }
                }
                else if (results.Count == 1)
                {
                    await ReplyAsync($"{results[0].Name}\n" +
                            $"{results[0].PriceUSD} USD\n" +
                            $"{results[0].StoreLink}");
                }
                else await ReplyAsync($"{game} was not found.");
            }
            catch
            {

            }
        }
    }
}
