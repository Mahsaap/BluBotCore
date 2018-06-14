using BluBotCore.Handlers;
using BluBotCore.Other;
using BluBotCore.Services;

using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.Net.Providers.WS4Net;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BluBotCore
{
    class Program
    {
        public static void Main(string[] args)
            => new DiscordClient().MainAsync().GetAwaiter().GetResult();
    }
}
