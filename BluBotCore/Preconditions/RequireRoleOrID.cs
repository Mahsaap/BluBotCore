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
        private readonly static List<ulong> _requiredRoleId = new(
            new ulong[] { Setup.DiscordStaffRole , Setup.DiscordWYKTVRole });
        private readonly static List<ulong> _requiredUserID = new(
            new ulong[] { DiscordIDs.Mahsaap, DiscordIDs.Space });

        public RequireRoleOrID()
        {
            //################### Not Required - Test before removing. ######################
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