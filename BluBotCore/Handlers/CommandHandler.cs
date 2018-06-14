﻿using Discord.Commands;
using Discord.WebSocket;

using System;
using System.Reflection;
using System.Threading.Tasks;

namespace BluBotCore.Handlers
{
    public class CommandHandler
    {
        private readonly IServiceProvider _service;
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;

        public CommandHandler(IServiceProvider service, DiscordSocketClient client, CommandService commands)
        {
            _client = client;
            _commands = commands;
            _service = service;

            _client.MessageReceived += HandleCommandAsync;
        }

        public async Task InstallCommandsAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(),_service);
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            if (message == null) return;
            int argPos = 0;
            if (!(message.HasCharPrefix('!', ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))) return;
            SocketCommandContext context = new SocketCommandContext(_client, message);
            IResult result = await _commands.ExecuteAsync(context, argPos, _service);
            //if (!result.IsSuccess)
            //{
            //    if (result.ErrorReason == "Unknown command.") return;
            //    await context.Channel.SendMessageAsync(result.ErrorReason);
            //}
        }
    }
}
