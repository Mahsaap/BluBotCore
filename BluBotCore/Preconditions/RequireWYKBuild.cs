using BluBotCore.Global;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace BluBotCore.Preconditions
{
    class RequireWYKBuild : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (Version.Build == BuildType.WYK.Value)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            else
                return Task.FromResult(PreconditionResult.FromError(""));
        }
    }
}