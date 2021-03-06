﻿using BluBotCore.Other;
using BluBotCore.Preconditions;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BluBotCore.Modules.Commands
{
    [Name("Admin")]
    [RequireContext(ContextType.Guild)]
    [RequireRoleOrID]
    public class AdminCmds : ModuleBase<SocketCommandContext>
    {
        [Command("version")]
        public async Task VersionAsync()
        {
            await ReplyAsync($"V{Version.Major}.{Version.Minor} > {Version.Build}");
        }

        //End application - ConsoleApp
        [Command("shutdown"), Summary("Shuts down the bot.")]
        public async Task ShutdownBotAsync()
        {
            await Context.Client.SetStatusAsync(UserStatus.Invisible);
            await Task.Delay(500);
            await ReplyAsync("**Shutting Down!**");
            Environment.Exit(1);
        }

        [Command("SetWYKTVRole")]
        public async Task WYKTVRoleSet(IRole role)
        {
            Setup.DiscordWYKTVRole = role.Id;
            await ReplyAsync($"WYKTV role set as {role.Name} - ({role.Id})");
            const string filename = "setup.txt";
            var tempLst = new List<string>()
            {
                Setup.DiscordAnnounceChannel.ToString(),
                Setup.DiscordStaffRole.ToString(),
                Setup.DiscordWYKTVRole.ToString()
            };
            File.WriteAllLines(filename, tempLst);
        }

        [Command("SetLiveChannel")]
        public async Task LiveChannelSet(IGuildChannel chan)
        {
            Setup.DiscordAnnounceChannel = chan.Id;
            await ReplyAsync($"Live Channel set as {chan.Name} - ({chan.Id})");
            const string filename = "setup.txt";
            var tempLst = new List<string>()
            {
                Setup.DiscordAnnounceChannel.ToString(),
                Setup.DiscordStaffRole.ToString(),
                Setup.DiscordWYKTVRole.ToString()
            };
            File.WriteAllLines(filename, tempLst);
        }

        [Command("SetLogChannel")]
        public async Task LogChannelSet(IGuildChannel chan)
        {
            Setup.DiscordLogChannel = chan.Id;
            await ReplyAsync($"Log Channel set as {chan.Name} - ({chan.Id})");
            const string filename = "setup.txt";
            var tempLst = new List<string>()
            {
                Setup.DiscordAnnounceChannel.ToString(),
                Setup.DiscordStaffRole.ToString(),
                Setup.DiscordWYKTVRole.ToString(),
                Setup.DiscordLogChannel.ToString()
            };
            File.WriteAllLines(filename, tempLst);
        }

        [Command("DisableLogChannel")]
        public async Task DisableLogChannel()
        {
            Setup.DiscordLogChannel = 0;
            await ReplyAsync($"Log Channel has been disabled.");
            const string filename = "setup.txt";
            var tempLst = new List<string>()
            {
                Setup.DiscordAnnounceChannel.ToString(),
                Setup.DiscordStaffRole.ToString(),
                Setup.DiscordWYKTVRole.ToString(),
                Setup.DiscordLogChannel.ToString()
            };
            File.WriteAllLines(filename, tempLst);
        }

        //botinfo
        [Command("botinfo"), Summary("Bot framework info.")]
        public async Task BotInfoAsync()
        {
            RestApplication application = await Context.Client.GetApplicationInfoAsync();

            EmbedBuilder eb = new EmbedBuilder()
            {
                Color = new Discord.Color(51, 102, 153),
                Title = "**__Bot Info__**",
                Description = ""
            };
            eb.AddField(x =>
            {
                x.Name = "**Author**";
                x.Value = $"{application.Owner.Username} (ID {application.Owner.Id})";
                x.IsInline = false;
            });
            eb.AddField(x =>
            {
                x.Name = "**Version**";
                x.Value = $"V{Version.Major}.{Version.Minor}";
                x.IsInline = true;
            });
            eb.AddField(x =>
            {
                x.Name = "**Variant**";
                x.Value = $"{Version.Build}";
                x.IsInline = true;
            });
            eb.AddField(x =>
            {
                x.Name = "**Libraries**";
                x.Value = "" +
                $"Discord.Net ({DiscordConfig.Version})\n" +
                "TwitchLib.API 3.1.4\n" +
                "StrawPollNet 1.0.2\n" +
                "SteamStoreQuery 1.0.4";
                x.IsInline = false;
            });
            eb.AddField(x =>
            {
                x.Name = "**Runtime**";
                x.Value = $"{RuntimeInformation.FrameworkDescription} {RuntimeInformation.OSArchitecture}";
                x.IsInline = true;
            });
            eb.AddField(x =>
            {
                x.Name = "**Stats**";
                x.Value = "" +
                $"Heap Size: {GetHeapSize()} MB\n" +
                $"Latency: {Context.Client.Latency}ms\n";
                x.IsInline = true;
            });
            string guildsStr = "";
            foreach (SocketGuild guild in Context.Client.Guilds)
            {
                guildsStr += $"**{guild.Name}** ({guild.Id})\n" +
                    $"- Members ({guild.MemberCount})\n" +
                    $"- Channels ({guild.Channels.Count})>(V{guild.VoiceChannels.Count})(T{guild.TextChannels.Count})\n" +
                    $"- Roles = {guild.Roles.Count}\n" +
                    $"- Owner = {guild.Owner}({guild.OwnerId})\n";
            }
            eb.AddField(x =>
            {
                x.Name = "**Guilds**";
                x.Value = $"{guildsStr}";
                x.IsInline = false;
            });
            eb.WithFooter(x =>
            {
                x.Text = $"Uptime: {GetUptime()}";
            });
            eb.WithCurrentTimestamp();
            await ReplyAsync("", embed: eb.Build());
        }

        //Purge messages
        [RequireBotPermission(GuildPermission.ManageMessages)]
        [Command("purge"), Summary("Purge messages.")]
        public async Task PurgeMsgAsync(int num)
        {
            if (num > 0 && num < 100) // Limit 100 - Minus 1 for your command to purge.
            {
                try
                {
                    var messages = Context.Channel.GetCachedMessages(num + 1);
                    ITextChannel chan = Context.Channel as ITextChannel;
                    await chan.DeleteMessagesAsync(messages);
                    await ReplyAsync($"{Context.User} has purged {messages.Count} messages!");
                    if (messages.Count < num)
                    {
                        await ReplyAsync("`Only cached messages can be removed.`");
                    }
                }
                catch { }
            }
            else await ReplyAsync($"Purge 99 messages max. [Cached ONLY].");
        }

        [Command("setplaying", RunMode = RunMode.Async)]
        public async Task SetPlayingAsync([Remainder]string entry)
        {
            await Context.Client.SetGameAsync(entry);
        }

        //set bot nick
        [RequireBotPermission(GuildPermission.ChangeNickname)]
        [Command("botnick")]
        [Summary("Change bot nickname.")]
        public async Task BotNickAsync([Remainder]string name)
        {
            IGuild guild = Context.Guild;
            IGuildUser self = await guild.GetCurrentUserAsync();
            await self.ModifyAsync(x => x.Nickname = name);
            await ReplyAsync($"Changed my nickname to `{name}`");
        }

        //Set a users nickname
        [RequireBotPermission(GuildPermission.ChangeNickname)]
        [Command("setusernick")]
        [Summary("Change a users nickname.")]
        public async Task UserNickAsync(IUser user, [Remainder]string name)
        {
            //IGuild guild = Context.Guild;
            IGuildUser usr = user as IGuildUser;
            await usr.ModifyAsync(x => x.Nickname = name);
            await ReplyAsync($"Changed {usr.Username}'s nickname to {name}");
        }

        //User Roles
        [Command("UserRoles"), Summary("Specified user's current roles.")]
        public async Task UserRolesAsync(IGuildUser user)
        {
            var roles = user.Guild.Roles;
            string title = $"{user.Username}'s Role List in {Context.Guild.Name}";
            StringBuilder strB = new StringBuilder();
            strB.AppendLine(title);
            int total = 0;
            foreach (IRole r in roles)
            {
                if (user.RoleIds.Contains(r.Id) && (r.Id != Context.Guild.EveryoneRole.Id))
                {
                    strB.AppendLine($"{r.Name} ({r.Id})");
                    total++;
                }
            }
            strB.Insert(title.Length, $" ({total} Total)" + Environment.NewLine);
            await ReplyAsync(strB.ToString());
        }

        //Uptime
        [Command("uptime"), Summary("Bot uptime.")]
        public async Task UptimeAsync()
        {
            await ReplyAsync($"Active for {GetUptime()}.");
        }

        //List Roles/Users per guild
        [Command("listroles"), Summary("Roles or users with specified role")]
        public async Task ListRoleAsync([Remainder]IRole irole = null)
        {
            SocketGuild guild = Context.Guild;
            StringBuilder strB = new StringBuilder();

            //no role specified - list all roles in the guild
            if (irole == null)
            {
                string title = $"{guild.Name} Role List:";
                strB.AppendLine(title);
                SocketRole everyone = guild.EveryoneRole;

                var listGuildRoles = new List<string>();

                foreach (IRole role in guild.Roles)
                {
                    if (role != everyone)
                        listGuildRoles.Add($"{role.Name} - ({role.Id})");
                }

                listGuildRoles.Sort();

                for (int i = 0; i < listGuildRoles.Count; i++)
                {
                    strB.AppendLine($"{listGuildRoles[i].Remove(1).ToUpper() + listGuildRoles[i].Substring(1)}");
                }

                strB.Insert(title.Length, $" ({listGuildRoles.Count} Total)" + Environment.NewLine);
                await ReplyAsync(strB.ToString());
            }
            else
            {
                string title = $"{guild.Name} {irole.Name} List:";
                strB.AppendLine(title);

                var listGuildUser = new List<Tuple<string, string>>();
                foreach (IGuildUser user in guild.Users)
                {
                    var roleIds = user.RoleIds;
                    if (roleIds.Contains(irole.Id))
                    {
                        listGuildUser.Add(new Tuple<string, string>(user.Username, user.Id.ToString()));
                    }
                }

                listGuildUser.Sort();

                for (int i = 0; i < listGuildUser.Count; i++)
                {
                    strB.AppendLine($"{listGuildUser[i].Item1} ({listGuildUser[i].Item2})");
                }

                strB.Insert(title.Length, $" ({listGuildUser.Count} Total)" + Environment.NewLine);
                await ReplyAsync(strB.ToString());
            }
        }

        //Multi link
        [Command("multi"), Summary("Urls for multi streams.")]
        public async Task MultiCastAsync([Remainder]string casters = null)
        {
            var casterList = casters.Split(' ');
            string result = "";
            foreach (string cast in casterList)
            {
                result += $"{cast}/";
            }
            if (casters != null)
                await ReplyAsync($"Check out the current multicasts via Multitwitch at http://multitwitch.tv/{result} or via Kadgar at http://kadgar.net/live/{result}");
        }

        //Caster
        [Command("caster"), Summary("Caster announce command.")]
        public async Task CasterAsync(string tname = null)
        {
            if (tname != null)
            {
                //string twitch = "https://www.twitch.tv/";
                await ReplyAsync($"You should check out {tname} over at https://www.twitch.tv/{tname}");
            }
        }

        //userinfo
        [Command("userinfo"), Alias("user", "whois"), Summary("Current or the specified user info.")]
        public async Task UserInfoAsync([Summary("The (optional) user to get info for")] SocketGuildUser user = null)
        {
            var userInfo = user ?? Context.User as SocketGuildUser;
            await ReplyAsync(
                $"User Info\n" +
                $"Username: {userInfo.Username}#{userInfo.Discriminator}\n" +
                $"User's ID: {userInfo.Id}\n" +
                $"User status: {userInfo.Status}\n" +
                $"IsBot: {userInfo.IsBot}\n" +
                $"Created On: {userInfo.CreatedAt}\n" +
                $"Currently Playing: {userInfo.Activity}\n" +
                $"Avatar Id: {userInfo.AvatarId}\n" +
                $"Avatar URL: {userInfo.GetAvatarUrl()}"
                );
        }

        //guildinfo
        [Command("serverinfo"), Alias("server", "guildinfo", "guild"), Summary("Guild info.")]
        public async Task ServerInfoAsync()
        {
            IGuild guild = Context.Guild;
            var users = await guild.GetUsersAsync();
            IGuildChannel defaultChan = await guild.GetChannelAsync(guild.DefaultChannelId);
            IGuildUser owner = await guild.GetOwnerAsync();
            await ReplyAsync(
                "Guild Info\n" +
                $"Name: {guild.Name}\n" +
                $"Owner's ID: {guild.OwnerId} {owner.Username}#{owner.Discriminator}\n" +
                $"Available: {guild.Available}\n" +
                $"Created On: {guild.CreatedAt}\n" +
                $"Default Channel ID: {guild.DefaultChannelId} {defaultChan.Name}\n" +
                $"User Count: {users.Count}\n" +
                $"Custom Emojis Count: {guild.Emotes.Count}\n" +
                $"Guild Roles Count: {guild.Roles.Count}\n" +
                $"Feature Count: {guild.Features.Count}\n" +
                $"Verification Level: {guild.VerificationLevel}\n" +
                $"MFA Level: {guild.MfaLevel}\n" +
                $"Embeddable: {guild.IsEmbeddable}\n" +
                $"Voice Region: {guild.VoiceRegionId}\n" +
                $"Splash ID: {guild.SplashId}\n" +
                $"Spalsh URL: {guild.SplashUrl}\n" +
                $"Icon ID: {guild.IconId}\n" +
                $"Icon URL: {guild.IconUrl}"
                );
        }

        //test
        [Command("test"), Alias("ping", "check"), Summary("Bot 'ping' test command.")]
        public async Task TestAsync()
        {
            string msg = $"Pong! - {Context.Client.Latency}ms";
            await ReplyAsync(msg);
        }

        
        [Command("encrypt")]
        [RequireContext(ContextType.DM)]
        [RequireRoleOrID]
        public async Task VerifyAsync(string entry)
        {
            string result = AES.Encrypt(entry);
            await ReplyAsync(result);
        }

        private static string GetUptime()
        => (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss");
        private static string GetHeapSize() => Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2).ToString();
    }
}


