using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BluBotCore.Modules.Commands
{
    //BROKEN ATM
    //[Name("Raffle")]
    //[RequireContext(ContextType.Guild)]
    public class RafflesCmds : InteractiveBase<SocketCommandContext>
    {
        private static List<ulong> entries = new List<ulong>();
        private static bool closed = false;
        private static string keyword;
        private static SocketUser raffleCreator;

        /*public*/ class RafflesGroup : InteractiveBase<SocketCommandContext>
        {
            [Command("raffle", RunMode = RunMode.Async)]
            public async Task RaffleAsync(string keywordEntry, int timeMin, [Remainder]string game)
            {
                entries.Clear();
                closed = false;
                keyword = keywordEntry;
                int time = timeMin;
                raffleCreator = Context.User;
                await ReplyAsync($"A raffle for `{game}` has started!\n" +
                    $"Type the keyword `{keyword}` in chat to join!\n" +
                    $"Rolling in {time} minutes unless closed early by {raffleCreator.Username}.");

                //closed raffle - Check
                while (!closed)
                {
                    if (time == 0) time = 120;
                    var entry = await NextMessageAsync(false, timeout: TimeSpan.FromMinutes(time));
                    //Check for keyword
                    if (entry.Content.Contains(keyword))
                    {
                        if (!entries.Contains(entry.Author.Id))
                        {
                            entries.Add(entry.Author.Id);
                        }
                    }
                    //Close the raffle Check - Only creator
                    //if (entry.Content.Contains("raffle close"))
                    //{
                    //    if (entry.Author.Id == Context.User.Id)
                    //    {
                    //        closed = true;
                    //    }
                    //}
                    //if (entry.Content.Contains("entered?"))
                    //{
                    //    if (entries.Contains(entry.Author.Id))
                    //    {
                    //        await ReplyAsync($"{entry.Author.Mention} confirmed!");
                    //    }
                    //    else
                    //    {
                    //        await ReplyAsync($"{entry.Author.Mention} negative! Please type the keyword `{keyword}` in chat");
                    //    }
                    //}
                    if (entry == null) { closed = true; }
                }

                if (closed)
                {
                    double totalEntries = entries.Count;
                    double chance = (1 / totalEntries) * 100;
                    await ReplyAsync($"The raffle for `{game}` has been closed!\n" +
                        $"Total entries = `{totalEntries}`\n" +
                        $"Chances of winning = `{chance}%`");
                    await Task.Delay(1000);

                    int seed = (DateTime.Now.Hour * 10000) + (DateTime.Now.Minute * 100) + (DateTime.Now.Second);
                    Random rnd = new Random(seed);
                    ulong winner = entries[rnd.Next(entries.Count)];
                    SocketGuildUser user = Context.Guild.GetUser(winner);
                    await ReplyAsync($"{user.Mention} you have won {game}!");
                }

            }

            [Command("confirm")]
            public async Task CheckEntryRaffleAsync()
            {
                if (entries.Contains(Context.User.Id))
                {
                    await ReplyAsync($"{Context.User.Mention} confirmed!");
                }
                else
                {
                    await ReplyAsync($"{Context.User.Mention} negative! Please type the keyword `{keyword}` in chat");
                }
            }

            [Command("close")]
            public void CloseRaffle()
            {
                if (Context.User.Id == raffleCreator.Id)
                {
                    closed = true;
                }
            }

        }


}
}
