using Discord.Addons.Interactive;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BluBotCore.Modules
{
    [Name("Conversion")]
    public class ConversionCmds : InteractiveBase<SocketCommandContext>
    {
        [Command("text")]
        [Alias("ascii")]
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
                String t = binary.Substring(i, 8);
                list.Add(Convert.ToByte(t, 2));
            }
            string text = Encoding.ASCII.GetString(list.ToArray());
            await ReplyAsync($"{text}");
        }

        [Command("binary")]
        [Alias("txt")]
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

        //[Command("time", RunMode = RunMode.Async)]
        //[Summary("")]
        //public async Task TimeAsync()
        //{
        //    await ReplyAsync("Please enter the time you want to convert " +
        //        "followed by the before and after abbreivations of the timezones to be used.\n" +
        //        "*Example usage: 08:20AM PDT CDT*");
        //    string format = "h:mmtt";
        //    SocketMessage response = await NextMessageAsync();
        //    var result = response.Content.Split(' ');
        //    if (result.Length != 3)
        //    {
        //        await ReplyAsync("You did not enter the required fields or you entered to many parameters, please try again.");
        //        return;
        //    }
        //    var setTime = DateTime.ParseExact(result[0], format, CultureInfo.InvariantCulture);
        //    if (String.IsNullOrEmpty(Timezones(result[1])) || String.IsNullOrEmpty(Timezones(result[2])))
        //    {
        //        await ReplyAsync(""); //Add abbreivation error
        //    }
        //    var beforeZone = TimeZoneInfo.FindSystemTimeZoneById(Timezones(result[1]));
        //    var utcTime = TimeZoneInfo.ConvertTimeToUtc(setTime, beforeZone);
        //    var afterZone = TimeZoneInfo.FindSystemTimeZoneById(Timezones(result[2]));
        //    var time = TimeZoneInfo.ConvertTimeFromUtc(utcTime, afterZone);
        //    await ReplyAsync($"" +
        //        $"{beforeZone.DisplayName} -> {setTime.ToShortTimeString()}" +
        //        $"\n" +
        //        $"{afterZone.DisplayName} -> {time.ToShortTimeString()}");
        //}

        private string Timezones(string zone)
        {
            switch (zone.ToUpper())
            {
                case "IDL":
                    return "Dateline Standard Time";
                case "HST":
                    return "Aleutian Standard Time";
                case "AST":
                    return "Atlantic Standard Time";
                case "PST":
                    return "Pacific Standard Time";
                default:
                    return "";
            }
        }
    }
}
