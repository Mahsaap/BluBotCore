﻿using BluBotCore.Services;
using System.Threading.Tasks;

namespace BluBotCore
{
    class Program
    {
        public static async Task Main()
        {
            DiscordClient _discord = new();
            await _discord.MainAsync();
        }
    }
}