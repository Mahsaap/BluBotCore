using BluBotCore.Other;
using BluBotCore.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;

namespace BluBotCore.Modules
{
    [Name("LiveMonitor")]
    [RequireContext(ContextType.Guild)]
    [Group("LM")]
    public class LiveMonitorCmds : ModuleBase<SocketCommandContext>
    {
        LiveMonitor _monitor;
        LiveMonitorCmds(LiveMonitor monitor)
        {
            _monitor = monitor;
        }

        [Command("List")]
        public async Task LMListAsync()
        {
            if (!(Context.User as IGuildUser).RoleIds.Contains(Setup.DiscordStaffRole) && !(Context.User.Id == Constants.Discord.Space) &&
                !(Context.User as IGuildUser).RoleIds.Contains(Setup.DiscordWYKTVRole) && !(Context.User.Id == Constants.Discord.Mahsaap)) return;
            if (_monitor.ChansName.Count <= 0) return;
            string result = $"**Casters being monitored:**\n";
            foreach (string entry in _monitor.ChansName)
            {
                result += $"{entry}\n";
            }
            await ReplyAsync(result);
        }

        [Command("count")]
        public async Task LMCountAsync()
        {
            if (!(Context.User as IGuildUser).RoleIds.Contains(Setup.DiscordStaffRole) && !(Context.User.Id == Constants.Discord.Space) &&
                !(Context.User as IGuildUser).RoleIds.Contains(Setup.DiscordWYKTVRole) && !(Context.User.Id == Constants.Discord.Mahsaap)) return;
            await ReplyAsync($"Currently monitoring {_monitor.ChansName.Count} channels!");

        }
        [Command("Update")]
        public async Task LMUpdateAsync()
        {
            if (!(Context.User as IGuildUser).RoleIds.Contains(Setup.DiscordStaffRole) && !(Context.User.Id == Constants.Discord.Space)
                && !(Context.User.Id == Constants.Discord.Mahsaap)) return;
            await _monitor.UpdateMonitorAsync();
        }

        [Command("addserver")]
        public async Task AddServer(string channelName, ulong channelID)
        {
            if (!(Context.User as IGuildUser).RoleIds.Contains(Setup.DiscordStaffRole) && !(Context.User.Id == Constants.Discord.Space)
                && !(Context.User.Id == Constants.Discord.Mahsaap)) return;
            string msg = "";
            if (!LiveMonitor.sepServerList.ContainsKey(channelName))
            {
                var chn = Context.Client.GetChannel(channelID) as SocketTextChannel;
                LiveMonitor.sepServerList.Add(channelName.ToLower(), channelID);
                msg = $"{channelName} has been added to live notifications and posting to {chn.Name} {channelID}";
            }
            else
            {
                msg = $"{channelName} is already in the list";
            }
            await ReplyAsync(msg);
        }

        [Command("removeserver")]
        public async Task RemoveServer(string channelName)
        {
            if (!(Context.User as IGuildUser).RoleIds.Contains(Setup.DiscordStaffRole) && !(Context.User.Id == Constants.Discord.Space)
                && !(Context.User.Id == Constants.Discord.Mahsaap)) return;
            string msg = "";
            if (LiveMonitor.sepServerList.ContainsKey(channelName))
            {
                LiveMonitor.sepServerList.Remove(channelName.ToLower());
                msg = $"{channelName} has been removed from live notifications";
            }
            else
            {
                msg = $"{channelName} is not in the list";
            }
            await ReplyAsync(msg);
        }
    }
}

