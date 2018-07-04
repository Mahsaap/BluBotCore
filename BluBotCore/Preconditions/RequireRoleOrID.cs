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
        /// <summary>Required mod roles for admin commands</summary>
        private static List<ulong> _requiredRoleId = new List<ulong>(
            new ulong[] { Setup.DiscordStaffRole , Setup.DiscordWYKTVRole });
        /// <summary>Required user w/o role for admin commands</summary>
        private static List<ulong> _requiredUserID = new List<ulong>(
            new ulong[] { Constants.Discord.Mahsaap, Constants.Discord.Space });

        public RequireRoleOrID()
        {
            //################### Not Required - Test before removing. ######################
        }

        /// <summary>Returns a PreconditionResult after evaluating the context with the requirements.</summary>
        /// <param name="context">Command context of the current message.</param>
        /// <param name="command">Command object</param>
        /// <param name="services">Injection service object</parem>
        /// <returns>PreconditionResult</returns>
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
