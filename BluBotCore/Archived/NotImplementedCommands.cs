using Discord.Commands;
using System.Threading.Tasks;

namespace BluBotCore.Other
{
    class NotImplementedCommands : ModuleBase<SocketCommandContext>
    {
        //Admin
        //ctt
        [Command("ctt"), Summary("Current ctt link.")]
        public async Task CttAsync(string url = null)
        {
            var urls = url ?? "";
            if (urls != "")
                await ReplyAsync($"Observe! Relay this message via the twitterbird to the friends! {url}");
        }

        ////1line random middle fingers
        //[Command("fu"), Summary("1Line Middle Fingers")]
        //public async Task MiddleFingersAsync()
        //{
        //    List<string> fu = new List<string>()
        //    {
        //    "┌∩┐(◣_◢)┌∩┐",
        //    "╭∩╮(Ο_Ο)╭∩╮",
        //    "‹^› ‹(•_•)› ‹^›",
        //    "┌∩┐(ಠ_ಠ)┌∩┐",
        //    "╭∩╮ʕ•ᴥ•ʔ╭∩╮",
        //    "ᶠᶸᶜᵏᵧₒᵤ"
        //    };
        //    int r = rnd.Next(fu.Count);
        //    await ReplyAsync(fu[r]);
        //}

        //[Group("usedcmds")]
        //public class UsedCmds : ModuleBase<SocketCommandContext>
        //{

        //    [Command("")]
        //    [Alias("all")]
        //    public async Task UsedCmdsAsync()
        //    {
        //        var cmds = CommandHandler.usedCommands;
        //        if (cmds == null) return;
        //        if (Context.Channel.Id != 301707775480954882) return;
        //        int count = 1;
        //        var result = "";

        //        foreach (var res in cmds)
        //        {
        //            result += $"**`{count}`**-" +
        //                $"**{res.GuildChan.Guild.Name}**({res.GuildChan.Guild.Id})-" +
        //                $"**{res.GuildChan.Name}**({res.GuildChan.Id})-" +
        //                $"**{res.User.Username}#{res.User.Discriminator}**({res.User.Id})-" +
        //                $"{res.Message}\n";
        //            count++;
        //        }
        //        await ReplyAsync("Used Commands:\n" + result);
        //    }

        //    [Command("user")]
        //    public async Task UsedUserCmdsAsync(IUser user)
        //    {
        //        var cmds = CommandHandler.usedCommands;
        //        if (cmds == null) return;
        //        if (Context.Channel.Id != 301707775480954882) return;
        //        int count = 1;
        //        var result = "";
        //        var u = (SocketUser)user;
        //        foreach (var usrCmd in cmds.Where(x => x.User.Id == u.Id))
        //        {
        //            result += $"**`{count}`**-" +
        //                $"**{usrCmd.GuildChan.Guild.Name}**({usrCmd.GuildChan.Guild.Id})-" +
        //                $"**{usrCmd.GuildChan.Name}**({usrCmd.GuildChan.Id})-" +
        //                $"**{usrCmd.User.Username}#{usrCmd.User.Discriminator}**({usrCmd.User.Id})-" +
        //                $"{usrCmd.Message}\n";
        //            count++;
        //        }
        //        await ReplyAsync($"Used Commands by {user}:\n" + result);
        //    }

        //    [Command("cmd")]
        //    public async Task UsedCmdCmdsAsync(string cmd)
        //    {
        //        var cmds = CommandHandler.usedCommands;
        //        if (cmds == null) return;
        //        if (Context.Channel.Id != 301707775480954882) return;
        //        int count = 1;
        //        var result = "";

        //        foreach (var usrCmd in cmds.Where(x => x.Message.ToLower().Contains(cmd.ToLower())))
        //        {
        //            result += $"**`{count}`**-" +
        //                $"**{usrCmd.GuildChan.Guild.Name}**({usrCmd.GuildChan.Guild.Id})-" +
        //                $"**{usrCmd.GuildChan.Name}**({usrCmd.GuildChan.Id})-" +
        //                $"**{usrCmd.User.Username}#{usrCmd.User.Discriminator}**({usrCmd.User.Id})-" +
        //                $"{usrCmd.Message}\n";
        //            count++;
        //        }
        //        await ReplyAsync($"Used Command {cmd}:\n" + result);
        //    }
        //}

        //[Command("move")]
        //public async Task MoveAsync()
        //{
        //    await ReplyAsync("test move!");
        //    var user = Context.Guild.GetUser(160582155142037505) as IGuildUser;
        //    //var user = await Context.Guild.GetUserAsync(160582155142037505);
        //    //var usr = (IGuildUser)Context.User;
        //    //var channel = usr.VoiceChannel;
        //    //if (user.VoiceChannel == null) return;
        //    //if ((IUser)user.Game. == StreamType.Twitch) check for streaming
        //    var channel = Context.Guild.GetVoiceChannel(188851528285683712);
        //    await user.ModifyAsync(x => x.ChannelId = channel.Id);
        //}

        /*
        [Command("movegroup")]
        public async Task MeetingAsync(ulong chan)
        {
            try
            {
                var curuser = Context.User as IGuildUser;
                var vchan = await Context.Guild.GetVoiceChannelAsync(curuser.VoiceChannel.Id);
                var users = vchan.GetUsersAsync();

                List<IGuildUser> usr = new List<IGuildUser>();

                foreach (var user in usr)
                {
                    await user.ModifyAsync(x => x.ChannelId = chan);
                }
            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.Message + "\n " + ex.StackTrace);
            }
        }
        */

        //[Command("meeting")]
        //public async Task MeetingAsync()
        //{
        //    try
        //    {
        //        var vChannelMeeting = Context.Guild.GetVoiceChannel(256161703098712083);
        //        var guild = Context.Guild as IGuild;
        //        var users = await guild.GetUsersAsync();
        //        foreach (var user in users)
        //        {
        //            foreach (var role in user.RoleIds)
        //            {
        //                if (role == Convert.ToUInt64(("Discord_Role_Mod_ID")))
        //                {
        //                    /*
        //                    if (user.VoiceChannel.Name == null)
        //                    {
        //                        //var dmChan = await user.CreateDMChannelAsync();
        //                        //await dmChan.SendMessageAsync($"It's Mod meeting time!\n" +
        //                        //    $"Your already here, so sit down and enjoy the ear bleeding!\n" +
        //                        //    $"{user.Username} has called this meeting!");
        //                        return;
        //                    }
        //                    else if (user.VoiceChannel.Id == null)
        //                    {
        //                        return;
        //                    }*/
        //                    if (user.VoiceSessionId != null)
        //                    {
        //                        await user.ModifyAsync(x => x.ChannelId = vChannelMeeting.Id);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await ReplyAsync(ex.Message + "\n " + ex.StackTrace);
        //    }
        //}
        //[Name("'Activity' Commands")]
        //[Group("activity")]
        //[Alias("act")]
        //public class Activity : ModuleBase
        //{

        //    [Command]
        //    public async Task ActivityAsync(IGuildUser user = null)
        //    {
        //        var usr = user ?? Context.User as IGuildUser;
        //        var act = Discord_Client.activity;
        //        await ReplyAsync($"{usr.Username}#{usr.Discriminator} has activity count of `{act[usr]}`.");
        //    }

        //    [Command("all")]
        //    [Alias("a")]
        //    public async Task ActivityAllAsync()
        //    {
        //        string result = "";
        //        string result1 = "";
        //        var act = Discord_Client.activity;
        //        Dictionary<string, int> temp = new Dictionary<string, int>();
        //        foreach (var usr in act.Keys.ToList())
        //        {
        //            temp.Add($"{usr.Username}#{usr.Discriminator}", act[usr]);
        //        }
        //        var keys = temp.Keys.ToList();
        //        keys.Sort();

        //        foreach (var user in keys)
        //        {
        //            if (result.Length <= 1950)
        //                result += $"{user} `{temp[user]}`\n";
        //            else
        //                result1 += $"{user} `{temp[user]}`\n";
        //        }
        //        await ReplyAsync(result);
        //        await ReplyAsync(result1);
        //    }

        //    [Command("top")]
        //    public async Task ActivityTopAsync(int count = 10)
        //    {
        //        string result = "";
        //        var act = Discord_Client.activity;
        //        List<Tuple<IGuildUser, int>> temp = new List<Tuple<IGuildUser, int>>();
        //        var items = from pair in act
        //                    orderby pair.Value descending
        //                    select pair;
        //        foreach (KeyValuePair<IGuildUser, int> pair in items)
        //        {
        //            temp.Add(new Tuple<IGuildUser, int>(pair.Key, pair.Value));
        //        }
        //        for (int a = 0; a < count; a++)
        //        {
        //            result += $"{temp[a].Item1} `{temp[a].Item2}`\n";
        //        }
        //        await ReplyAsync(result);
        //    }
        //}

        //[Command("userperm")]
        //public async Task UserPermAsync(IUser usr, [Optional] IGuildChannel channel)
        //{
        //    var user = (SocketGuildUser)(usr ?? Context.User);
        //    var chan = channel ?? (IGuildChannel)Context.Channel;
        //    var uPerm = user.GetPermissions(chan);

        //    var ebPerm = new EmbedBuilder(){ Color = new Color(114, 137, 218) };

        //    ebPerm.AddField(x =>{ x.Name = "RawValue"; x.Value = uPerm.RawValue.ToString(); x.IsInline = false; });
        //    ebPerm.AddField(x =>{ x.Name = "Create Instant Invite"; x.Value = uPerm.CreateInstantInvite.ToString(); x.IsInline = true; });
        //    ebPerm.AddField(x =>{ x.Name = "Manage Channel"; x.Value = uPerm.ManageChannel.ToString(); x.IsInline = false; });
        //    ebPerm.AddField(x =>{ x.Name = "Manage Roles"; x.Value = uPerm.ManageRoles.ToString(); x.IsInline = true; });
        //    ebPerm.AddField(x =>{ x.Name = "Manage Webhooks"; x.Value = uPerm.ManageWebhooks.ToString(); x.IsInline = false; });
        //    ebPerm.AddField(x =>{ x.Name = "Read Messages"; x.Value = uPerm.ReadMessages.ToString(); x.IsInline = true; });
        //    ebPerm.AddField(x =>{ x.Name = "Send Messages"; x.Value = uPerm.SendMessages.ToString(); x.IsInline = false; });
        //    ebPerm.AddField(x =>{ x.Name = "Send TTS Messages"; x.Value = uPerm.SendTTSMessages.ToString(); x.IsInline = true; });
        //    ebPerm.AddField(x =>{ x.Name = "Manage Messages"; x.Value = uPerm.ManageMessages.ToString(); x.IsInline = false; });
        //    ebPerm.AddField(x =>{ x.Name = "Embed Links"; x.Value = uPerm.EmbedLinks.ToString(); x.IsInline = true; });
        //    ebPerm.AddField(x =>{ x.Name = "Attach Files"; x.Value = uPerm.AttachFiles.ToString(); x.IsInline = false; });
        //    ebPerm.AddField(x =>{ x.Name = "Read Message History"; x.Value = uPerm.ReadMessageHistory.ToString(); x.IsInline = true; });
        //    ebPerm.AddField(x =>{ x.Name = "Mention Everyone"; x.Value = uPerm.MentionEveryone.ToString(); x.IsInline = false; });
        //    ebPerm.AddField(x =>{ x.Name = "Use External Emojis"; x.Value = uPerm.UseExternalEmojis.ToString(); x.IsInline = true; });
        //    ebPerm.AddField(x =>{ x.Name = "Add Reactions"; x.Value = uPerm.AddReactions.ToString(); x.IsInline = false; });

        //    await ReplyAsync($"User({ user.Username}#{user.Discriminator}) permissions for channel({chan.Name}) in guild ({chan.Guild.Name})", embed: ebPerm.Build());

        //}
    }
}
