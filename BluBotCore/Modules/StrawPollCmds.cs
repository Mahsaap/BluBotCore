using BluBotCore.Preconditions;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using StrawPollNET.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BluBotCore.Modules
{
    [Name("StrawPoll")]
    [RequireContext(ContextType.Guild)]
    public class StrawPollCmds : InteractiveBase
    {
        [Group("sp")]
        public class StrawPollGroup : InteractiveBase<SocketCommandContext>
        {
            private static CreatedPoll currentPoll;
            private static List<CreatedPoll> pastPolls = new List<CreatedPoll>();

            [RequireRoleOrID]
            [Command("create", RunMode = RunMode.Async)]
            public async Task SPCreateAsync()
            {
                SocketMessage title = await ResponseAsync(Context.Channel, "**Enter Title**"); //Verify empty check fail or not
                SocketMessage options = await ResponseAsync(Context.Channel, "**Enter Options (Seperated by a comma)**");
                string[] optsArray = options.Content.Split(',');

                if (optsArray.Length < 2)
                {
                    await ReplyAsync("**At least 2 options are required. Aborting!**");
                }
                bool mult;
                SocketMessage multi = await ResponseAsync(Context.Channel, "**Multiple Entries? y or n**");
                switch (multi.Content.ToLower())
                {
                    case "y":
                    case "yes":
                        mult = true;
                        break;
                    case "n":
                    case "no":
                        mult = false;
                        break;
                    default:
                        await ReplyAsync("**You did not enter yes or no. Aborting!**");
                        return;
                }
                EmbedBuilder eb = new EmbedBuilder()
                {
                    Color = new Color(114, 137, 218),
                    Description = " "
                };
                eb.AddField(x =>
                {
                    x.Name = "Title";
                    x.Value = title.Content;
                    x.IsInline = false;
                });
                string optsStr = "";
                List<string> optsList = new List<string>();
                foreach (string opt in optsArray)
                {
                    optsStr += $"{opt}\n";
                    optsList.Add(opt);
                }
                eb.AddField(x =>
                {
                    x.Name = "Options";
                    x.Value = optsStr;
                    x.IsInline = false;
                });
                eb.AddField(x =>
                {
                    x.Name = "Multiple Entries";
                    x.Value = mult;
                    x.IsInline = false;
                });
                await ReplyAsync("**Verify StrawPoll**", embed: eb.Build());
                SocketMessage confirm = await ResponseAsync(Context.Channel, "**Is this correct? y or n**");
                bool confir;
                switch (confirm.Content.ToLower())
                {
                    case "y":
                    case "yes":
                        confir = true;
                        break;
                    case "n":
                    case "no":
                        confir = false;
                        break;
                    default:
                        await ReplyAsync("**You did not enter yes or no. Aborting!**");
                        return;
                }
                if (confir)
                {
                    currentPoll = await CreatePoll(title.Content, optsList, mult, StrawPollNET.Enums.DupCheck.Normal, false);
                    pastPolls.Insert(0,currentPoll);
                    if (pastPolls.Count == 6) pastPolls.RemoveAt(5);
                    await ReplyAsync($"**{title} - {currentPoll.PollUrl}**");
                }
                else
                {
                    await ReplyAsync("**Please try again.**");
                    return;
                }
            }

            [Command("results")]
            [Alias("check")]
            public async Task SPCheckAsync()
            {
                if (currentPoll == null) return;
                FetchedPoll result = await GetPoll(currentPoll.Id);
                string res = $"**{currentPoll.Title}**\n";
                foreach (var r in result.Results)
                {
                    res += $"{r.Key} - {r.Value}\n";
                }
                await ReplyAsync(res);
            }

            //[Command("pastresults")]
            //public async Task SearchPollAsync()
            //{
            //    if (pastPolls.Count == 0) return;
            //    string result = $"**Past 5 StrawPolls\n";
            //    int i = 1;
            //    foreach (CreatedPoll poll in pastPolls)
            //    {
            //        result += $"[{i}] - {poll.Title} <{poll.PollUrl}>\n";
            //        i++;
            //    }
            //    IUserMessage message = await ReplyAsync(result);
            //}

            private async Task<SocketMessage> ResponseAsync(ISocketMessageChannel channel, string info)
            {
                    await channel.SendMessageAsync(info);
                    SocketMessage response = await NextMessageAsync();
                    return response;
            }

            private async Task<StrawPollNET.Models.CreatedPoll> CreatePoll(string title, List<string> options, bool multi, StrawPollNET.Enums.DupCheck dupCheck, bool captcha)
            {
                return await StrawPollNET.API.Create.CreatePollAsync(title, options, multi, dupCheck, captcha);
            }
            private async Task<StrawPollNET.Models.FetchedPoll> GetPoll(int pollId)
            {
                return await StrawPollNET.API.Get.GetPollAsync(pollId);
            }
        }


    }
}
