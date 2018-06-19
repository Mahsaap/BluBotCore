using BluBotCore.Other;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BluBotCore.Preconditions
{
    class RequireRoleOrID : PreconditionAttribute
    {
        private static List<ulong> _requiredRoleId = new List<ulong>(
            new ulong[] { Setup.DiscordStaffRole , Setup.DiscordWYKTVRole }); // List of RoleIds required to use a command
        private static List<ulong> _requiredUserID = new List<ulong>(
            new ulong[] { Constants.Discord.Mahsaap, Constants.Discord.Space }); // List of UserIds required to bypass Role check

        public RequireRoleOrID()
        {
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (_requiredUserID.Contains(context.User.Id))
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            else if (context.User is IGuildUser && _requiredRoleId.Any(x => (context.User as IGuildUser).RoleIds.Contains(x)))
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            else
                return Task.FromResult(PreconditionResult.FromError("You do not have the required role or user ID"));
        }
    }
}
