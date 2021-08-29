using BluBotCore.Preconditions;
using Discord.Commands;
using System.Threading.Tasks;

namespace BluBotCore.Modules.Commands
{
    [Name("WYK")]
    [Group("wyk")]
    [RequireWYKBuild]
    public class WYKCmds : ModuleBase<SocketCommandContext>
    {
        [Command("social")]
        [Summary("WYKTV Website/FB/Twitter/Youtube links.")]
        public async Task WYKAsync()
        {
            await ReplyAsync("**Website =** <http://www.wouldyoukindly.com/> \n" +
                "**Twitch** =** <https://www.twitch.tv/team/wyktv> \n" +
                "**Instagram =** <https://www.instagram.com/wyktv/> \n" +
                "**Twitter =** <https://twitter.com/wouldyoukindly> \n" +
                "**YouTube =** <https://www.youtube.com/channel/UCDfmQq5QEUeg7fjfRADcsvw>");
        }
    }
}