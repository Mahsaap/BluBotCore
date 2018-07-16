using BluBotCore.Other;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Parameters;
using TwitchLib.Api;
using TwitchLib.Api.Exceptions;
using TwitchLib.Api.Models.v5.Teams;
using TwitchLib.Api.Models.v5.Channels;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;

namespace BluBotCore.Services
{
    public class LiveMonitor
    {
        #region Private Variables
            private readonly DiscordSocketClient _client;
            private readonly CommandService _commands;
            private readonly IServiceProvider _service;

            private static DateTime _onlineTime;
            private static readonly byte[] TWITCH_PINK_SCREEN_CHECKSUM = new byte[] { 11, 241, 144, 174, 218, 192, 175, 31, 120, 108, 52, 36, 55, 174, 200, 134, 12, 8, 223, 245, 175, 184, 76, 16, 140, 201, 39, 57, 123, 39, 23, 78 };
            private static readonly int TWITCH_PINK_SCREEN_RETRY_ATTEMPTS = 3;
            private static readonly int TWITCH_PINK_SCREEN_RETRY_DELAY = 15000; //ms

            #region Lists
                private static List<string> _chansName = new List<string>();
                private static List<string> _chansID = new List<string>();
            #endregion

            #region Dictionaries
                private static ConcurrentDictionary<string, Tuple<RestUserMessage, string, string>> _sepliveEmbeds = new ConcurrentDictionary<string, Tuple<RestUserMessage, string, string>>();
                private static ConcurrentDictionary<string, Tuple<RestUserMessage,string,string>> _liveEmbeds = new ConcurrentDictionary<string, Tuple<RestUserMessage,string,string>>();
        #endregion
        #endregion

        #region Public Variables
        public static Dictionary<string, ulong> sepServerList = new Dictionary<string, ulong>();
        #endregion

        #region Properties

        public LiveStreamMonitor Monitor { get; private set; }
        public TwitchAPI API { get; private set; }
        public List<String> ChansName { get => _chansName; }
        public List<String> ChansID { get => _chansID; }
        #endregion

        public LiveMonitor(IServiceProvider service, DiscordSocketClient client, CommandService commands)
        {
            _client = client;
            _service = service;
            _commands = commands;

            Task.Run(() => ConfigLiveMonitorAsync());
        }

        private async Task ConfigLiveMonitorAsync()
        {
            var time = DateTime.Now.ToString("HH:MM:ss");
            Console.WriteLine($"{time} Monitor     Checking if Discord is connected!");
            while (_client.ConnectionState != ConnectionState.Connected)
            {
                var timeWait = DateTime.Now.ToString("HH:MM:ss");
                Console.WriteLine($"{timeWait} Monitor     Waiting 2 seconds.");
                await Task.Delay(2000);
            }
            try
            {
                API = new TwitchAPI();
                try
                {
                    API.Settings.ClientId = AES.Decrypt(Cred.TwitchAPIID);
                    API.Settings.AccessToken = AES.Decrypt(Cred.TwitchAPIToken);
                }
                catch (Exception ex)
                {
                    if (ex is TokenExpiredException)
                    {
                        var mahsaap = _client.GetUser(Constants.Discord.Mahsaap) as IUser;
                        await mahsaap.SendMessageAsync("TwitchLib token has expired.");
                        var token = await API.Auth.v5.RefreshAuthTokenAsync(
                            AES.Decrypt(Cred.TwitchAPIRefreshToken), AES.Decrypt(Cred.TwitchAPIToken), AES.Decrypt(Cred.TwitchAPIID));
                        await mahsaap.SendMessageAsync("TwitchLib token has been refreshed.");
                        string dataOld;
                        List<string> tmpList = new List<string>();
                        using (StreamReader file = new StreamReader("init.txt"))
                        {
                            while ((dataOld = file.ReadLine()) != null)
                                tmpList.Add(dataOld);
                            file.Close();
                        }
                        tmpList[2] = AES.Encrypt(token.AccessToken);
                        Cred.TwitchAPIToken = AES.Encrypt(token.AccessToken);
                        tmpList[3] = AES.Encrypt(token.RefreshToken);
                        Cred.TwitchAPIRefreshToken = AES.Encrypt(token.RefreshToken);
                        File.WriteAllLines("init.txt", tmpList);
                        await mahsaap.SendMessageAsync($"TwitchLib keys have been updated in file. Expires in {token.ExpiresIn}.");

                        API.Settings.ClientId = AES.Decrypt(Cred.TwitchAPIID);
                        API.Settings.AccessToken = AES.Decrypt(Cred.TwitchAPIToken);
                        Console.WriteLine($"{time} Monitor     Tokens have been refreshed,updated and started");

                    }
                }

                Monitor = new LiveStreamMonitor(API, 120, invokeEventsOnStart: false);

                Console.WriteLine($"{time} Monitor     Instance Created");

                await SetCastersAsync();

                Monitor.OnStreamOnline += _monitor_OnStreamOnline;
                Monitor.OnStreamMonitorStarted += _monitor_OnStreamMonitorStarted;
                Monitor.OnStreamsSet += _monitor_OnStreamsSet;
                Monitor.OnStreamOffline += _monitor_OnStreamOffline;
                Monitor.OnStreamUpdate += _monitor_OnStreamUpdate;

                Monitor.StartService(); //Keep at the end!

                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
            }
        }

        private async void _monitor_OnStreamOnline(object sender, OnStreamOnlineArgs e)
        {
            string url = @"https://www.twitch.tv/" + e.Stream.Channel.Name;
            EmbedBuilder eb = SetupLiveEmbed($":link: {e.Stream.Channel.DisplayName}", $"{e.Stream.Channel.Status}", $"{e.Stream.Channel.Game}",
                e.Stream.Preview.Medium + Guid.NewGuid().ToString(), e.Stream.Channel.Logo, url);

            string time = DateTime.Now.ToString("HH:MM:ss");
            Console.WriteLine($"{time} Monitor     {e.Stream.Channel.DisplayName} is live playing {e.Stream.Game}");

            string twitterTag = FindTwitterTag(e.Stream.Channel.DisplayName);

            string twitterURL = await TweetMessageAsync($"{e.Stream.Channel.DisplayName} is live playing {e.Stream.Game}! {e.Stream.Channel.Url} {twitterTag}#WYKTV", e.Stream.Preview.Medium + Guid.NewGuid().ToString(), e.Stream.Channel.Name.ToLower());

            await SetupEmbedMessageAsync(eb, e, null, twitterURL);
        }

        private async void _monitor_OnStreamUpdate(object sender, OnStreamUpdateArgs e)
        {
            if (_liveEmbeds.ContainsKey(e.ChannelId))
            {
                if (_client.ConnectionState == ConnectionState.Connected)
                {
                    if (Setup.DiscordAnnounceChannel == 0) return;
                    var msg = _liveEmbeds[e.ChannelId];
                    if (msg.Item2 != e.Stream.Channel.Status || msg.Item3 != e.Stream.Channel.Game)
                    {
                        EmbedBuilder eb = SetupLiveEmbed($":link: {e.Stream.Channel.DisplayName}", $"{e.Stream.Channel.Status}", $"{e.Stream.Channel.Game}",
                            e.Stream.Preview.Medium + Guid.NewGuid().ToString(), e.Stream.Channel.Logo, @"https://www.twitch.tv/" + e.Stream.Channel.Name);

                        await UpdateNotificationAsync(eb, _liveEmbeds, e);
                        await Task.Delay(500);
                        await UpdateNotificationAsync(eb, _sepliveEmbeds, e);

                        string time = DateTime.Now.ToString("HH:MM:ss");
                        Console.WriteLine($"{time} Monitor     Stream {e.Channel} updated");
                    }
                }
            }
        }

        private async Task UpdateNotificationAsync(EmbedBuilder eb, ConcurrentDictionary<string, Tuple<RestUserMessage, string, string>> lst, OnStreamUpdateArgs e)
        {
            if (lst.ContainsKey(e.ChannelId))
            {
                var msg = lst[e.ChannelId];
                await msg.Item1.ModifyAsync(x => x.Embed = eb.Build());
                lst[e.ChannelId] = new Tuple<RestUserMessage, string, string>(msg.Item1, e.Stream.Channel.Status, e.Stream.Channel.Game);
            }
        }

        private async void _monitor_OnStreamOffline(object sender, OnStreamOfflineArgs e)
        {
            string time = DateTime.Now.ToString("HH:MM:ss");
            Console.WriteLine($"{time} Monitor     {e.Channel} is offline");

            await StreamOfflineAsync(_liveEmbeds, e, 250);
            await StreamOfflineAsync(_sepliveEmbeds, e, 500);

        }

        private async Task StreamOfflineAsync(ConcurrentDictionary<string, Tuple<RestUserMessage, string, string>> lst, OnStreamOfflineArgs e, int delay)
        {
            if (lst.ContainsKey(e.ChannelId))
            {
                await Task.Delay(delay);
                RestUserMessage embed = lst[e.ChannelId].Item1;
                await DeleteEmbed(embed);
                lst.TryRemove(e.ChannelId, out Tuple<RestUserMessage, string, string> outResult);
            }
        }

        private void _monitor_OnStreamsSet(object sender, OnStreamsSetArgs e)
        {
            string time = DateTime.Now.ToString("HH:MM:ss");
            Console.WriteLine($"{time} Monitor     Streams Set!");
        }

        private async void _monitor_OnStreamMonitorStarted(object sender, OnStreamMonitorStartedArgs e)
        {
            _onlineTime = DateTime.Now;
            string time = DateTime.Now.ToString("HH:MM:ss");
            Console.WriteLine($"{time} Monitor     Started");
            _liveEmbeds.Clear();
            _sepliveEmbeds.Clear();

            if (_client.ConnectionState == ConnectionState.Connected)
            {
                if (Setup.DiscordAnnounceChannel == 0) return;
                var chan = _client.GetChannel(Setup.DiscordAnnounceChannel) as SocketTextChannel;

                var messages = await chan.GetMessagesAsync().FlattenAsync();
                if (messages.Count() != 0) await chan.DeleteMessagesAsync(messages);

                foreach (var sepServer in sepServerList)
                {
                    var sepChan = _client.GetChannel(sepServer.Value) as SocketTextChannel;
                    var sapMes = await sepChan.GetMessagesAsync().FlattenAsync();
                    if (sapMes.Count() != 0) await sepChan.DeleteMessagesAsync(sapMes);
                }

                foreach (var x in Monitor.CurrentLiveStreams)
                {
                    EmbedBuilder eb = SetupLiveEmbed($":link: {x.Channel.DisplayName}", $"{x.Channel.Status}", $"{x.Channel.Game}",
                    x.Preview.Medium + Guid.NewGuid().ToString(), x.Channel.Logo, @"https://www.twitch.tv/" + x.Channel.Name);

                    Console.WriteLine($"{time} Monitor     {x.Channel.DisplayName} is live playing {x.Game}");
                    await Task.Delay(1000);
                    await SetupEmbedMessageAsync(eb, null, x, "");
                }
            }
        }

        private async Task DeleteEmbed(RestUserMessage msg)
        {
            if (_client.ConnectionState == ConnectionState.Connected)
                await msg.DeleteAsync();
        }

        private EmbedBuilder SetupLiveEmbed(string title, string description, string value, string image, string thumbnail, string url)
        {
            EmbedBuilder eb = new EmbedBuilder()
            {
                Color = new Discord.Color(51, 102, 153),
                Title = title,
                Description = description,
                Url = url
            };
            eb.AddField(x =>
            {
                x.Name = $"Playing";
                x.Value = value;
                x.IsInline = false;
            });
            eb.WithImageUrl(image);
            eb.WithThumbnailUrl(thumbnail);
            eb.WithFooter(x =>
            {
                x.Text = "Twitch.tv";
            });
            eb.WithCurrentTimestamp();
            return eb;
        }

        public async Task SetCastersAsync()
        {
            //Team team = await API.Teams.v5.GetTeamAsync("wyktv");

            //foreach (Channel user in team.Users)
            //{
            //    _chansName.Add(user.Name);
            //    _chansID.Add(user.Id);
            //}
            //Monitor.SetStreamsByUserId(_chansID);


            //Testing
            List<string> testList = new List<string>() { "mahsaap" };
            var user = await API.Users.v5.GetUserByNameAsync("mahsaap");
            var testUser = await API.Channels.v5.GetChannelByIDAsync(user.Matches[0].Id);
            _chansID.Add(testUser.Id);
            _chansName.Add(testUser.Name);
            Monitor.SetStreamsByUserId(_chansID);
        }

        public async Task UpdateMonitorAsync()
        {
            Monitor.StopService();
            _chansName.Clear();
            _chansID.Clear();
            await SetCastersAsync();
            Monitor.StartService();
        }


        private async Task SetupEmbedMessageAsync(EmbedBuilder eb, OnStreamOnlineArgs e, TwitchLib.Api.Models.v5.Streams.Stream s, string _twitterURL)
        {
            string twitchURL = "";
            string channelID = "";
            string channelName = "";
            string status = "";
            string game = "";

            if (e != null)
            {
                twitchURL = e.Stream.Channel.Url;
                channelID = e.Stream.Channel.Id;
                channelName = e.Stream.Channel.DisplayName;
                status = e.Stream.Channel.Status;
                game = e.Stream.Channel.Game;
            }
            else if (s != null)
            {
                twitchURL = s.Channel.Url;
                channelID = s.Channel.Id;
                channelName = s.Channel.DisplayName;
                status = s.Channel.Status;
                game = s.Channel.Game;
            }
            else return;
            if (Setup.DiscordAnnounceChannel == 0) return;
            if (_client.ConnectionState == ConnectionState.Connected)
            {
                string here = "";
                if (_onlineTime.AddSeconds(30) <= DateTime.Now) here = "@here ";
                if (_twitterURL.Length > 1) here += $"\nTwitter (*<{_twitterURL}>*)"; else here += " ";
                here += $"\nTwitch (*{twitchURL}*)";
                here = here.Insert(0, $"**{channelName} is live!** ");

                await SendEmbedAsync(Setup.DiscordAnnounceChannel, eb, _liveEmbeds, here, channelID, channelName, status, game);

                if (sepServerList.ContainsKey(channelName.ToLower()))
                {
                    await Task.Delay(500);
                    await SendEmbedAsync(sepServerList[channelName.ToLower()], eb, _sepliveEmbeds, here, channelID, channelName, status, game);
                }
            }
        }

        private async Task SendEmbedAsync(ulong id, EmbedBuilder eb, ConcurrentDictionary<string, Tuple<RestUserMessage, string, string>> lst, string here, string channelID, string channelName, string status, string game)
        {
            var chan = _client.GetChannel(id) as SocketTextChannel;
            RestUserMessage msg = await chan.SendMessageAsync(here, embed: eb.Build());
            if (lst.ContainsKey(channelID)) lst[channelID] = new Tuple<RestUserMessage, string, string>(msg, status, game);
            else lst.TryAdd(channelID, new Tuple<RestUserMessage, string, string>(msg, status, game));
        }


        private async Task<String> TweetMessageAsync(string text, string url, string channel)
        {
            try
            {
                WebClient webClient = new WebClient();
                byte[] image = null;
                //attempt to get an image 10 times
                for (int i = 0; i < TWITCH_PINK_SCREEN_RETRY_ATTEMPTS; i++)
                {
                    image = webClient.DownloadData(url);
                    byte[] hash;
                    using (var sha256 = System.Security.Cryptography.SHA256.Create())
                    {
                        hash = sha256.ComputeHash(image);
                    }
                    if (hash.SequenceEqual(TWITCH_PINK_SCREEN_CHECKSUM))
                    {
                        //pink screen detected. Lets sleep for X seconds and try again. 
                        Console.WriteLine($"{DateTime.Now.ToString("HH:MM:ss")} Twitter     Detected Pink Screen for {url}, trying again in {TWITCH_PINK_SCREEN_RETRY_DELAY}, attempt {i+1} out of {TWITCH_PINK_SCREEN_RETRY_ATTEMPTS}" );
                        await Task.Delay(TWITCH_PINK_SCREEN_RETRY_DELAY);
                    } else
                    {
                        //not a pink screen. Break out of loop
                        break;
                    }
                }

                var publishOptions = new PublishTweetOptionalParameters();
                publishOptions.MediaBinaries.Add(image);
                var twitterObject = await TweetAsync.PublishTweet(text, publishOptions);
                return twitterObject.Url;

            }
            catch (Exception ex)
            {
                var mahsaap = _client.GetUser(Constants.Discord.Mahsaap) as IUser;
                await mahsaap.SendMessageAsync(ex.Message + "\n" + ex.StackTrace);
                //cannot return null - code checks for length > 1
                return "";
            }
        }

        private static TimeSpan GetUptime()
            => (DateTime.Now - Process.GetCurrentProcess().StartTime);

        private string FindTwitterTag(string channel)
        {
            switch (channel.ToLower())
            {
                case "brotatoe":
                    return "@JayBrotatoe ";
                case "domesticdan":
                    return "@DomesticDan ";
                case "farringtonempire":
                    return "@OfficialFarEm ";
                case "firecrow":
                    return "@FirecrowTV ";
                case "goobers515":
                    return "@Goobers515 ";
                case "inexpensivegamer":
                    return "@InexpensiveGamR ";
                case "littlesiha":
                    return "@littlesiha ";
                case "overboredgaming":
                    return "@OverBoredGaming ";
                case "robanddan":
                    return "@RobAndDan ";
                case "romcomm":
                    return "@Romcommm ";
                case "themavshow":
                    return "@TheMavShow ";
                default:
                    return "";
            }
        }
    }
}
