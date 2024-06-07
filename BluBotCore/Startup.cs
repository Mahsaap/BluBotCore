using BluBotCore.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BluBotCore
{
    internal class Startup
    {
        private IServiceCollection _collection;
        private DiscordClient _discordClient;
        Startup(IServiceCollection collection, DiscordClient discord)
        {
            _collection = collection;
            _discordClient = discord;
            
        }
    }
}
