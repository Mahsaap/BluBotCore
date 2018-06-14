using BluBotCore.Other;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using System.Linq;
using Tweetinvi;

namespace BluBotCore.Modules
{
    [Name("Twitter")]
    [RequireContext(ContextType.Guild)]
    public class TwitterCmds : ModuleBase<SocketCommandContext>
    {
        [Command("tweet")]
        public async Task TweetTextAsync([Remainder]string text)
        {
            if (!(Context.User as IGuildUser).RoleIds.Contains(Setup.DiscordStaffRole) && !(Context.User.Id == 101614418915266560)
                && !(Context.User.Id == 88798728948809728)) return;
            //280 char max
            if (text.Length < 280)
            {
                try
                {
                    await TweetAsync.PublishTweet(text);
                }
                catch
                {
                    await ReplyAsync("Twitter Client not responding. You may need to restart the program to fix.");
                }
                await ReplyAsync("Tweet sent!\n" +
                    $"`{text}`");
            }
            else
            {
                await ReplyAsync("To many characters [280 max], please try again.");
            }
        }
    }
}
