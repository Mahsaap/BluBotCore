using BluBotCore.Preconditions;
using BluBotCore.Services;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BluBotCore.Modules.Commands
{
    [Name("LiveMonitor")]
    [RequireContext(ContextType.Guild)]
    [RequireRoleOrID]
    [Group("LM")]
    public class LiveMonitorCmds : ModuleBase<SocketCommandContext>
    {
        private readonly LiveMonitor _monitor;

        public LiveMonitorCmds(LiveMonitor monitor)
        {
            _monitor = monitor;
        }

        [Command("List")]
        [Alias("Channels")]
        public async Task LMListAsync()
        {
            if (LiveMonitor.MonitoredChannels.Count <= 0) return;
            string result = $"**{LiveMonitor.MonitoredChannels.Count} casters are being monitored:**\n```";
            foreach (string entry in LiveMonitor.MonitoredChannels.Keys)
            {
                result += $"{entry}\n";
            }
            result += "```";
            await ReplyAsync(result);
        }

        [Command("Update")]
        public async Task LMUpdateAsync(string channel = null)
        {
            var msg = await ReplyAsync("Updating...");
            bool complete = await _monitor.UpdateMonitorAsync(channel);
            
            if (complete && msg != null && channel == null)
            {
                await msg.ModifyAsync(x => x.Content = "Updated all channel posts.");
            }
            else if (complete && msg != null)
            {
                await msg.ModifyAsync(x => x.Content = $"Updated {channel}'s post!");
            }
            else
            {
                await msg.ModifyAsync(x => x.Content = "Update FAILED! Please try again....");
            }
        }

        [Command("Remove")]
        public async Task LMRemoveAsync(string channel)
        {
            bool complete = await _monitor.RemoveLiveEmbedAsync(channel);
            if (complete)
            {
                await ReplyAsync($"Removed {channel}'s post!");
            }
            else
            {
                await ReplyAsync("Remove FAILED! Please try again....");
            }
        }
    }
}

