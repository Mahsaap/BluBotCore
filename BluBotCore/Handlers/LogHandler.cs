using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace BluBotCore.Handlers
{
    class LogHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _service;
        public LogHandler(IServiceProvider service, DiscordSocketClient client, CommandService commands)
        {
            _client = client;
            _service = service;
            _commands = commands;

            _client.Log += Log;
            _commands.Log += Log;
        }
        private Task Log(LogMessage msg)
        {
            switch (msg.Severity)
            {
                case LogSeverity.Critical:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    break;
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.DarkBlue;
                    break;
            }

            if (msg.Severity == LogSeverity.Error || msg.Severity == LogSeverity.Warning || msg.Severity == LogSeverity.Critical)
            {
                string msge = msg.ToString();
                if (msg.ToString().ToLower().Contains("unexpected close")) msge = msg.Message;
                var mahsaap = _client.GetUser(88798728948809728) as IUser;
                mahsaap.SendMessageAsync(msge);
            }
            Console.WriteLine(msg.ToString());
            Console.ResetColor();
            return Task.CompletedTask;
        }
    }
}
