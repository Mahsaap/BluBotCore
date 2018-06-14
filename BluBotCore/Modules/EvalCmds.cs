
using Discord;
using Discord.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Threading.Tasks;

namespace BluBotCore.Modules
{
    //needs to be fixed fails infiinte loops etc...
    //[Name("Eval")]
    //[RequireContext(ContextType.Guild)]
    /*public*/
    class EvalCmds : ModuleBase<SocketCommandContext>
    {
        [Command("Eval")]
        public async Task EvalExpressionAsync([Remainder]string expression)
        {
            IUserMessage waitMsg = await ReplyAsync("one moment please.....");
            try
            {
                object result = await CSharpScript.EvaluateAsync(expression, ScriptOptions.Default.WithImports("System.Math"));
                await waitMsg.ModifyAsync(x =>
                {
                    x.Content = $"The result of `{ expression}` is `{ result.ToString()}`";
                });
            }
            catch (CompilationErrorException e)
            {
                await waitMsg.ModifyAsync(x =>
                {
                    string failResult = $"Unable to compile. Please try again!\n";
                    foreach (Diagnostic entry in e.Diagnostics)
                    {
                        failResult += $"{entry.Descriptor.Description}`\n";
                    }
                    x.Content = failResult;
                });
            }
        }
    }
}
