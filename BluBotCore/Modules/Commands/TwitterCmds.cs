using BluBotCore.Preconditions;
using Discord.Commands;
using System.Threading.Tasks;
using Tweetinvi;

namespace BluBotCore.Modules.Commands
{
    [Name("Twitter")]
    [RequireContext(ContextType.Guild)]
    [RequireRoleOrID]
    public class TwitterCmds : ModuleBase<SocketCommandContext>
    {
        [Command("tweet")]
        public async Task TweetTextAsync([Remainder]string text)
        {
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
