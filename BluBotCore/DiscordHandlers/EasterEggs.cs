using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BluBotCore.DiscordHandlers
{
    class EasterEggs
    {
        public static bool easterEggEnable = true;
        private Random rnd = new Random();

        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _service;

        private List<ulong> eggChk = new List<ulong>();

        public EasterEggs(IServiceProvider service, DiscordSocketClient client)
        {
            _client = client;
            _service = service;

            //_client.MessageReceived += _client_MessageReceived;
        }

        private async Task _client_MessageReceived(SocketMessage message)
        {
            if (message.Author.Id == _client.CurrentUser.Id) return;
            var chan = message.Channel as SocketTextChannel;

            #region EasterEggs
            if ((DateTime.Now.Second == 32 || DateTime.Now.Second == 13 || DateTime.Now.Second == 38) && easterEggEnable)
            {
                int r = rnd.Next(99);
                if (r < 20)
                {
                    if (eggChk.Contains(message.Author.Id)) return;
                    switch (message.Author.Id)
                    {
                        //case 98944716304818176: //Crash // Triggered on May16 #General @ 8:53pm Atlantic Time
                            //await SendMessage(chan, @"https://media.giphy.com/media/3o6ZtcaQX0Xa8FPLX2/giphy.gif", "Crash, we will never know!");
                            //break;
                        case 103600594459062272: //Brawli
                            await SendMessage(chan, @"https://media.giphy.com/media/3o6gDULDiTel9fUC8E/giphy.gif", "Brawli, the first man to have birthdays everyday!");
                            eggChk.Add(message.Author.Id);
                            await DMAsync(message.Author.Username);
                            break;
                        case 110593521731268608: //Dagwood
                            await SendMessage(chan, @"https://media.giphy.com/media/x43pXtJShv93a/giphy.gif", "Sentient Beard!");
                            eggChk.Add(message.Author.Id);
                            await DMAsync(message.Author.Username);
                            break;
                        case 93467935502237696: //Romcomm
                            await SendMessage(chan, @"https://media.giphy.com/media/iuKAW0oDUMwXm/giphy.gif", "Rom, how is Persona 5 going?");
                            eggChk.Add(message.Author.Id);
                            await DMAsync(message.Author.Username);
                            break;
                        case 98639175951056896: //Domestic
                            await SendMessage(chan, @"https://media.giphy.com/media/6Nt5hRfsh3g4g/giphy.gif", "DD, can I has cookies?");
                            eggChk.Add(message.Author.Id);
                            await DMAsync(message.Author.Username);
                            break;
                        case 88036532950171648: //Dan R&D
                            await SendMessage(chan, @"https://media.giphy.com/media/byGsNfk0bVRMA/giphy.gif", "Finish your paperwork Dan!");
                            eggChk.Add(message.Author.Id);
                            await DMAsync(message.Author.Username);
                            break;
                        case 89765494566965248: //Farrington
                            await SendMessage(chan, @"https://media.giphy.com/media/TBddd797slSxO/giphy.gif", "Farrinton is all about the top hats and rainbow beards!");
                            eggChk.Add(message.Author.Id);
                            await DMAsync(message.Author.Username);
                            break;
                        case 80164863602601984: //Goobers
                            await SendMessage(chan, @"https://media.giphy.com/media/2GVVvT5ATZtUQ/giphy.gif", "Goobers, no one on the team is louder!");
                            eggChk.Add(message.Author.Id);
                            await DMAsync(message.Author.Username);
                            // https://media.giphy.com/media/3ov9jELey7dcyUKfuM/giphy.gif
                            break;
                        case 92434060126752768: //Firecrow
                            await SendMessage(chan, @"https://media.giphy.com/media/jufOz6eRjBwLS/giphy.gif", "Firecrow likes to spice it up with spicy food!");
                            eggChk.Add(message.Author.Id);
                            await DMAsync(message.Author.Username);
                            break;
                        case 90531924837163008: //Brotatoe
                            await SendMessage(chan, @"https://media.giphy.com/media/l1J9HIAGYwsFHTOmc/giphy.gif", "Brotatoe loves pineapple pizza!");
                            eggChk.Add(message.Author.Id);
                            await DMAsync(message.Author.Username);
                            break;
                        case 140284587208081408: //Littlesiha
                            await SendMessage(chan, @"https://media.giphy.com/media/3o7qE2VAxuXWeyvJIY/giphy.gif", "Siha is the best dancer on the team!");
                            eggChk.Add(message.Author.Id);
                            await DMAsync(message.Author.Username);
                            break;
                        case 100288457162637312: //Rob R&D
                            //
                            await SendMessage(chan, @"https://media.giphy.com/media/mPPu9VhiS9vos/giphy.gif", "Rob the Ocean Man!");
                            eggChk.Add(message.Author.Id);
                            await DMAsync(message.Author.Username);
                            // https://media.giphy.com/media/Wo2HWPDqBbPpK/giphy.gif
                            break;
                        case 110864542690447360: //Ruu
                            await SendMessage(chan, @"https://media.giphy.com/media/1Rhv5tW0ZewcU/giphy.gif", "Mr Ruu Sir Gamer with the cheapest deals on the team!");
                            eggChk.Add(message.Author.Id);
                            await DMAsync(message.Author.Username);
                            // https://media.giphy.com/media/4UAe1eyooZ5II/giphy.gif
                            break;
                        case 102471664238870528: //Mav
                            await SendMessage(chan, @"https://media.giphy.com/media/3ohhwsQ8hLTIoPcF0c/giphy.gif", "Mav, the silly one!");
                            eggChk.Add(message.Author.Id);
                            await DMAsync(message.Author.Username);
                            break;
                        default:
                            break;
                    }
                }
            }
            #endregion
        }


        private async Task SendMessage(SocketTextChannel chan, string url = null, string saying = null)
        {
            if (url != null && saying != null)
            {
                var eb = BuildEmbed(url);
                await chan.SendMessageAsync(saying, embed: eb.Build());
            }
            else if (url != null && saying == null)
            {
                var eb = BuildEmbed(url);
                await chan.SendMessageAsync("", embed: eb.Build());
            }
            else if (url == null && saying != null)
            {
                await chan.SendMessageAsync(saying);
            }
        }

        private EmbedBuilder BuildEmbed(string url)
        {
            EmbedBuilder eb = new EmbedBuilder()
            {
                ImageUrl = url
            };
            return eb;
        }

        private async Task DMAsync(string user)
        {
            var mahsaap = _client.GetUser(Constants.Discord.Mahsaap) as IUser;
            await mahsaap.SendMessageAsync($"Easter Egg for {user} has triggered!");
        }
    }
}
