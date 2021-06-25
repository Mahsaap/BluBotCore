using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BluBotCore.Modules.Commands
{
    [Name("Conversion")]
    public class ConversionCmds : ModuleBase<SocketCommandContext>
    {
        [Command("text")]
        [Alias("ascii,txt")]
        [Summary("Convert Binary to Text(Ascii)")]
        public async Task BinaryToTxtAsync([Remainder]string binary)
        {
            string lineSeparator = ((char)0x2028).ToString();
            string paragraphSeparator = ((char)0x2029).ToString();
            binary = binary.Replace(" ", string.Empty)
                .Replace("\r\n", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("\r", string.Empty)
                .Replace(lineSeparator, string.Empty)
                .Replace(paragraphSeparator, string.Empty);
            var list = new List<Byte>();
            for (int i = 0; i < binary.Length; i += 8)
            {
                string t = binary.Substring(i, 8);
                list.Add(Convert.ToByte(t, 2));
            }
            string text = Encoding.ASCII.GetString(list.ToArray());
            await ReplyAsync($"{text}");
        }

        [Command("binary")]
        [Summary("Convert Text(Ascii) to Binary")]
        public async Task TxtToBinaryAsync([Remainder]string text)
        {
            string result = "";
            byte[] byteArray = Encoding.ASCII.GetBytes(text);
            for (int i = 0; i < byteArray.Length; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    if (j == 8) result += " ";
                    else
                    {
                        result += (byteArray[i] & 0x80) > 0 ? "1" : "0";
                        byteArray[i] <<= 1;
                    }
                }
            }
            await ReplyAsync(result);
        }
    }
}
