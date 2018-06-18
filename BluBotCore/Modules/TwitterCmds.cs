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
            if (!(Context.User as IGuildUser).RoleIds.Contains(Setup.DiscordStaffRole) && !(Context.User.Id == Constants.Discord.Space)
                && !(Context.User.Id == Constants.Discord.Mahsaap)) return;
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
