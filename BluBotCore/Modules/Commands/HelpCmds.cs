using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BluBotCore.Modules.Commands
{
    [Name("Help")]
    public class HelpCmds : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _commands;
        private readonly IServiceProvider _map;
        //private List<string> ignoreList = new List<string>();
        private readonly List<string> hideList = new List<string>();

        public HelpCmds(IServiceProvider map, CommandService commands)
        {
            _commands = commands;
            _map = map;
            //ignoreList.Add("wyk");
            //ignoreList.Add("sp");
            hideList.Add("Admin");
            hideList.Add("LiveMonitor");
            hideList.Add("Owner");
            hideList.Add("Help");
        }

        [Command("help")]
        [Alias("commands","cmds")]
        [Summary("Lists this bot's commands.")]
        public async Task Help(string path = "")
        {
            EmbedBuilder output = new EmbedBuilder()
            {
                Color = new Discord.Color(51, 102, 153)
            };
            if (path == "")
            {
                output.Title = "**__WYKTV-Bot Commands__**";

                foreach (var mod in _commands.Modules.Where(m => m.Parent == null))
                {
                    AddHelp(mod, ref output);
                }

                output.Footer = new EmbedFooterBuilder
                {
                    Text = "Use 'help <module>' to get help with a module."
                };
            }
            else
            {
                var mod = _commands.Modules.FirstOrDefault(m => m.Name.Replace("Module", "").ToLower() == path.ToLower());
                if (mod == null) { await ReplyAsync("No module could be found with that name."); return; }

                output.Title = $"__{mod.Name}__";
                output.Description = $"{mod.Summary}\n" +
                (!string.IsNullOrEmpty(mod.Remarks) ? $"({mod.Remarks})\n" : "") +
                //(mod.Aliases.Any() ? $"{string.Join(",", mod.Aliases)}\n" : "") +
                (mod.Submodules.Any() ? $"Submodules: {mod.Submodules.Select(m => m.Name)}\n" : "") + " ";
                AddCommands(mod, ref output);
            }

            await ReplyAsync("", embed: output.Build());
        }

        public void AddHelp(ModuleInfo module, ref EmbedBuilder builder)
        {
            if (hideList.Contains(module.Name)) return;
            foreach (var sub in module.Submodules) AddHelp(sub, ref builder);

            builder.AddField(f =>
            {
                f.Name = $"{module.Name}";
                if (!hideList.Contains(module.Name))
                {
                    string subValue = "";
                    if (module.Submodules.Count != 0) subValue += "Submodules: ";
                    f.Value = $"{string.Join(", ", module.Submodules.Select(m => m.Name))}\n";
                    string cmdValue = "";
                    if (module.Submodules.Count != 0) cmdValue += "Commands: ";
                    f.Value += $"{string.Join(", ", module.Commands.Select(x => $"`{x.Name}`"))}";
                }
                else f.Value = "-";
            });
        }

        public void AddCommands(ModuleInfo module, ref EmbedBuilder builder)
        {
            foreach (var command in module.Commands)
            {
                command.CheckPreconditionsAsync(Context, _map).GetAwaiter().GetResult();
                AddCommand(command, ref builder);
            }

        }

        public void AddCommand(CommandInfo command, ref EmbedBuilder builder)
        {
            builder.AddField(f =>
            {
                f.Name = $"{command.Summary}";
                f.Value = (!string.IsNullOrEmpty(command.Remarks) ? $"({command.Remarks})\n" : "") +
                (command.Aliases.Any() ? $"Aliases: {string.Join(", ", command.Aliases.Select(x => $"`{x}`"))}\n" : "");
            });
        }

        public string GetAliases(CommandInfo command)
        {
            StringBuilder output = new StringBuilder();
            if (!command.Parameters.Any()) return output.ToString();
            foreach (var param in command.Parameters)
            {
                if (param.IsOptional)
                    output.Append($"[{param.Name} = {param.DefaultValue}] ");
                else if (param.IsMultiple)
                    output.Append($"|{param.Name}| ");
                else if (param.IsRemainder)
                    output.Append($"...{param.Name} ");
                else
                    output.Append($"<{param.Name}> ");
            }
            return output.ToString();
        }
        public string GetPrefix(CommandInfo command)
        {
            var output = /*GetPrefix(command.Module)*/ "";
            output += $"{command.Aliases.FirstOrDefault()} ";
            return output;
        }
        public string GetPrefix(ModuleInfo module)
        {
            string output = "";
            if (module.Parent != null) output = $"{output}";
            if (module.Aliases.Any())
                output += string.Concat(module.Aliases.FirstOrDefault(), " ");
            return output;
        }
    }
}
