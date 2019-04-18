using BluBotCore.Preconditions;
using BluBotCore.Services;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace BluBotCore.Modules.Commands
{
    [Name("LiveMonitor")]
    [RequireContext(ContextType.Guild)]
    [RequireRoleOrID]
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
            if (_monitor.MonitoredChannels.Count <= 0) return;
            string result = $"**{_monitor.MonitoredChannels.Count} casters are being monitored:**\n```";
            foreach (string entry in _monitor.MonitoredChannels.Keys)
            {
                result += $"{entry}\n";
            }
            result += "```";
            await ReplyAsync(result);
        }

        [Command("Update")]
        public async Task LMUpdateAsync()
        {
            await _monitor.UpdateMonitorAsync();
        }
    }
}

