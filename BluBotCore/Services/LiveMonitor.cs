using BluBotCore.Constants;
using BluBotCore.Other;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Parameters;
using TwitchLib.Api;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.V5.Models.Channels;
using TwitchLib.Api.V5.Models.Teams;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;

namespace BluBotCore.Services
{
    public class LiveMonitor
    {
        #region Private Variables

            /// <summary>
            /// Discord Client instance
            /// </summary>
            private readonly DiscordSocketClient _client;

            /// <summary>
            /// Debug Option - Toggle twitter tweets on/off
            /// </summary>
            readonly static bool twitterEnabled = true;

            /// <summary>
            /// Time the bot comes online.
            /// This is used to not tweet / @everyone current online streamers on load.
            /// </summary>
            private static DateTime _botOnlineTime;
            
            /// <summary>
            /// Dictionary of the current team members of WYKTV being monitored.
            /// </summary>
            private static Dictionary<string,string> _monitoredChannels = new Dictionary<string, string>();

            /// <summary>
            /// Dictionary of all the live discord messages.
            /// Concurrent since this can be added/removed from at anytime.
            /// </summary>
            private static ConcurrentDictionary<string, Tuple<RestUserMessage,string,string>> _liveEmbeds = new ConcurrentDictionary<string, Tuple<RestUserMessage,string,string>>();

        #endregion

        #region Properties

            /// <summary>
            /// Live Monitor Instance.
            /// </summary>
            public LiveStreamMonitorService Monitor { get; private set; }

            /// <summary>
            /// Twitch API Instance.
            /// </summary>
            public TwitchAPI API { get; private set; }

            /// <summary>
            /// Dictionary of all the current monitored channels.
            /// </summary>
            public Dictionary<String,String> MonitoredChannels { get => _monitoredChannels; }

        #endregion

        /// <summary>
        /// Constructor
        /// Injects Dicord Client instance.
        /// Starts Live Monitor configuration.
        /// </summary>
        /// <param name="client"></param>
        public LiveMonitor(DiscordSocketClient client)
        {
            _client = client;

            Task.Run(() => ConfigLiveMonitorAsync());
        }

        /// <summary>
        /// Live Monitor configuration. 
        /// </summary>
        /// <returns></returns>
        private async Task ConfigLiveMonitorAsync()
        {
            var time = DateTime.Now.ToString("HH:MM:ss");
            Console.WriteLine($"{time} Monitor     Checking if Discord is connected!");

            // Ensure Discord is connected before config continues. Loop every 2 seconds till online.
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
                    // Set Credentials in Twitch API Config.
                    API.Settings.ClientId = AES.Decrypt(Cred.TwitchAPIID);
                    API.Settings.AccessToken = AES.Decrypt(Cred.TwitchAPIToken);
                }
                catch (Exception ex)
                {
                    // Token Expired Refresh Sequence.
                    if (ex is TokenExpiredException)
                    {
                        var mahsaap = (_client.GetUser(Constants.Discord.Mahsaap) as IUser);
                        await mahsaap.SendMessageAsync("TwitchLib token has expired.");

                        // Refresh Token
                        var token = await API.V5.Auth.RefreshAuthTokenAsync(
                            AES.Decrypt(Cred.TwitchAPIRefreshToken), AES.Decrypt(Cred.TwitchAPIToken), AES.Decrypt(Cred.TwitchAPIID));
                        await mahsaap.SendMessageAsync("TwitchLib token has been refreshed.");
                        
                        // Grab old credentials from file.
                        List<string> tmpList = new List<string>();
                        using (StreamReader file = new StreamReader("init.txt"))
                        {
                            string dataOld;
                            while ((dataOld = file.ReadLine()) != null)
                                tmpList.Add(dataOld);
                            file.Close();
                        }

                        // Set new credentials to file.
                        tmpList[2] = AES.Encrypt(token.AccessToken);
                        Cred.TwitchAPIToken = AES.Encrypt(token.AccessToken);
                        tmpList[3] = AES.Encrypt(token.RefreshToken);
                        Cred.TwitchAPIRefreshToken = AES.Encrypt(token.RefreshToken);

                        // Save (overwrite) the file.
                        File.WriteAllLines("init.txt", tmpList);

                        await mahsaap.SendMessageAsync($"TwitchLib keys have been updated in file. Expires in {token.ExpiresIn}.");

                        // Set Credentials in Twitch API Config.
                        API.Settings.ClientId = AES.Decrypt(Cred.TwitchAPIID);
                        API.Settings.AccessToken = AES.Decrypt(Cred.TwitchAPIToken);
                        Console.WriteLine($"{time} Monitor     Tokens have been refreshed and updated!");

                    }
                }

                Monitor = new LiveStreamMonitorService(API, 300);

                Console.WriteLine($"{time} Monitor     Instance Created");

                await SetCastersAsync();

                // Events
                Monitor.OnStreamOnline += Monitor_OnStreamOnline;
                Monitor.OnStreamOffline += Monitor_OnStreamOffline;
                Monitor.OnStreamUpdate += Monitor_OnStreamUpdate;
                Monitor.OnServiceStarted += Monitor_OnServiceStarted;
                Monitor.OnChannelsSet += Monitor_OnChannelsSet;
                
                Monitor.Start();

                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
            }
        }

        private void Monitor_OnStreamOnline(object sender, OnStreamOnlineArgs e)
        {
            Task.Run(() => Monitor_OnStreamOnlineAsync(e)).Wait();
            Task.Delay(1000);
        }
        private async Task Monitor_OnStreamOnlineAsync(OnStreamOnlineArgs e)
        {
            try{
            var ee = await API.V5.Streams.GetStreamByUserAsync(e.Channel);

            if (!_liveEmbeds.ContainsKey(ee.Stream.Channel.Id)){
                string url = @"https://www.twitch.tv/" + ee.Stream.Channel.Name;
                EmbedBuilder eb = SetupLiveEmbed($":link: {ee.Stream.Channel.DisplayName}", $"{ee.Stream.Channel.Status}", $"{ee.Stream.Channel.Game}",
                    ee.Stream.Preview.Medium + Guid.NewGuid(), ee.Stream.Channel.Logo, url);

                string time = DateTime.Now.ToString("HH:MM:ss");
                Console.WriteLine($"{time} Monitor     {ee.Stream.Channel.DisplayName} is live playing {ee.Stream.Game}");

                string twitterTag = FindTwitterTag(ee.Stream.Channel.DisplayName);

                string twitterURL = "";
                if (twitterEnabled){
                    twitterURL = await TweetMessageAsync($"{ee.Stream.Channel.DisplayName} is live playing {ee.Stream.Game}! {ee.Stream.Channel.Url} {twitterTag}#WYK", ee.Stream.Preview.Medium + Guid.NewGuid().ToString(), ee.Stream.Channel.Name.ToLower());
                }
                await Task.Delay(1000);
                await SetupEmbedMessageAsync(eb, ee, null, twitterURL);
            }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }


        private void Monitor_OnStreamUpdate(object sender, OnStreamUpdateArgs e)
        {
            Task.Run(() => Monitor_OnStreamUpdateAsync(e));
            Task.Delay(1000);
        }
        private async Task Monitor_OnStreamUpdateAsync(OnStreamUpdateArgs e)
        {
            try {
                var ee = await API.V5.Streams.GetStreamByUserAsync(e.Channel);
                if (_liveEmbeds.ContainsKey(e.Channel))
                {
                    if (_client.ConnectionState == ConnectionState.Connected)
                    {
                        if (Setup.DiscordAnnounceChannel == 0) return;
                        var msg = _liveEmbeds[e.Channel];
                        if (msg.Item2 != ee.Stream.Channel.Status || msg.Item3 != ee.Stream.Channel.Game)
                        {
                            EmbedBuilder eb = SetupLiveEmbed($":link: {ee.Stream.Channel.DisplayName}", $"{ee.Stream.Channel.Status}", $"{ee.Stream.Channel.Game}",
                                ee.Stream.Preview.Medium + Guid.NewGuid().ToString(), ee.Stream.Channel.Logo, @"https://www.twitch.tv/" + ee.Stream.Channel.Name);

                            await UpdateNotificationAsync(eb, _liveEmbeds, e);

                            string time = DateTime.Now.ToString("HH:MM:ss");
                            Console.WriteLine($"{time} Monitor     Stream {ee.Stream.Channel.DisplayName} updated");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private async Task UpdateNotificationAsync(EmbedBuilder eb, ConcurrentDictionary<string, Tuple<RestUserMessage, string, string>> lst, OnStreamUpdateArgs e)
        {
            var ee = await API.V5.Streams.GetStreamByUserAsync(e.Channel);
            if (lst.ContainsKey(e.Channel))
            {
                var msg = lst[e.Channel];
                await msg.Item1.ModifyAsync(x => x.Embed = eb.Build());
                lst[e.Channel] = new Tuple<RestUserMessage, string, string>(msg.Item1, ee.Stream.Channel.Status, ee.Stream.Channel.Game);
            }
        }

        private void Monitor_OnStreamOffline(object sender, OnStreamOfflineArgs e)
        {
            Task.Run(() => Monitor_OnStreamOfflineAsync(e));
            Task.Delay(1000);
        }
        private async void Monitor_OnStreamOfflineAsync(OnStreamOfflineArgs e)
        {
            var ee = await API.V5.Channels.GetChannelByIDAsync(e.Channel);
            string time = DateTime.Now.ToString("HH:MM:ss");
            Console.WriteLine($"{time} Monitor     {ee.DisplayName} is offline");

            if (_liveEmbeds.ContainsKey(e.Channel))
            {
                await Task.Delay(250);
                RestUserMessage embed = _liveEmbeds[e.Channel].Item1;
                if (_client.ConnectionState == ConnectionState.Connected)
                    await embed.DeleteAsync();
                _liveEmbeds.TryRemove(e.Channel, out Tuple<RestUserMessage, string, string> outResult);
            }

        }

        private void Monitor_OnChannelsSet(object sender, TwitchLib.Api.Services.Events.OnChannelsSetArgs e)       
        {
            string time = DateTime.Now.ToString("HH:MM:ss");
            Console.WriteLine($"{time} Monitor     Streams Set!");
        }


        private async void Monitor_OnServiceStarted(object sender, TwitchLib.Api.Services.Events.OnServiceStartedArgs e)
        {
            _botOnlineTime = DateTime.Now;
            string time = DateTime.Now.ToString("HH:MM:ss");
            Console.WriteLine($"{time} Monitor     Started");
            _liveEmbeds.Clear();

            var livestreamers = await API.V5.Streams.GetLiveStreamsAsync(Monitor.ChannelsToMonitor);            
            

            if (_client.ConnectionState == ConnectionState.Connected)
            {
                if (Setup.DiscordAnnounceChannel == 0) return;
                var chan = _client.GetChannel(Setup.DiscordAnnounceChannel) as SocketTextChannel;

                var messages = await chan.GetMessagesAsync().FlattenAsync();
                try
                {
                    if (messages.Count() != 0) await chan.DeleteMessagesAsync(messages);
                }
                catch
                {
                    foreach (var mes in messages)
                    {
                        await Task.Delay(500);
                        await mes.DeleteAsync();
                    }
                }

                foreach (var x in livestreamers.Streams)
                {
                    var xx = await API.V5.Streams.GetStreamByUserAsync(x.Channel.Id);
                    EmbedBuilder eb = SetupLiveEmbed($":link: {xx.Stream.Channel.DisplayName}", $"{xx.Stream.Channel.Status}", $"{xx.Stream.Channel.Game}",
                    xx.Stream.Preview.Medium + Guid.NewGuid().ToString(), xx.Stream.Channel.Logo, @"https://www.twitch.tv/" + xx.Stream.Channel.Name);

                    Console.WriteLine($"{time} Monitor     {xx.Stream.Channel.DisplayName} is live playing {xx.Stream.Game}");
                    await Task.Delay(1000);
                    await SetupEmbedMessageAsync(eb, null, xx.Stream, "");
                }
            }
        }

        private string NullEmptyCheck(string entry)
        {
            if (!String.IsNullOrEmpty(entry))
            {
                return entry;
            }
            else 
                return ".";
        }

        private EmbedBuilder SetupLiveEmbed(string title, string description, string game, string image, string thumbnail, string url)
        {
            title = NullEmptyCheck(title);
            description = NullEmptyCheck(description);
            game = NullEmptyCheck(game);

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
                x.Value = game;
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
            Team team = await API.V5.Teams.GetTeamAsync("wyktv");

            foreach (Channel user in team.Users)
            {
                _monitoredChannels.Add(user.DisplayName, user.Id);
            }
            Monitor.SetChannelsById(_monitoredChannels.Values.ToList());
        }

        public async Task UpdateMonitorAsync()
        {
            Monitor.Stop();
            _monitoredChannels.Clear();
            await SetCastersAsync();
            Monitor.Start();
        }


        private async Task SetupEmbedMessageAsync(EmbedBuilder eb, TwitchLib.Api.V5.Models.Streams.StreamByUser e, TwitchLib.Api.V5.Models.Streams.Stream s, string _twitterURL)
        {
            if (s == null && e == null || eb == null) return;
            TwitchLib.Api.V5.Models.Streams.StreamByUser ee = null;
            if (e != null)
            {
                ee = await API.V5.Streams.GetStreamByUserAsync(e.Stream.Channel.Id);
            }

            string twitchURL = ee?.Stream.Channel.Url ?? s?.Channel.Url;
            string channelID = ee?.Stream.Channel.Id ?? s?.Channel.Id;
            string channelName = ee?.Stream.Channel.DisplayName ?? s?.Channel.DisplayName;
            string status = ee?.Stream.Channel.Status ?? s?.Channel.Status;
            string game = ee?.Stream.Channel.Game ?? s?.Channel.Game;            

            if (Setup.DiscordAnnounceChannel == 0) return;
            if (_client.ConnectionState == ConnectionState.Connected)
            {
                string here = "";
                if (_botOnlineTime.AddSeconds(30) <= DateTime.Now) here = "@here ";
                if (_twitterURL.Length > 1) here += $"\nTwitter (*<{_twitterURL}>*)"; else here += " ";
                here += $"\nTwitch (*{twitchURL}*)";
                here = here.Insert(0, $"**{channelName} is live!** ");

                await SendEmbedAsync(Setup.DiscordAnnounceChannel, eb, here, channelID, status, game);
            }
        }

        private async Task SendEmbedAsync(ulong id, EmbedBuilder eb, string here, string channelID, string status, string game)
        {
            var chan = _client.GetChannel(id) as SocketTextChannel;
            RestUserMessage msg = await chan.SendMessageAsync(here, embed: eb.Build());
            if (_liveEmbeds.ContainsKey(channelID)) _liveEmbeds[channelID] = new Tuple<RestUserMessage, string, string>(msg, status, game);
            else _liveEmbeds.TryAdd(channelID, new Tuple<RestUserMessage, string, string>(msg, status, game));
            await Task.CompletedTask;
        }


        private async Task<String> TweetMessageAsync(string text, string url, string channel)
        {
            try
            {
                WebClient webClient = new WebClient();
                byte[] image = webClient.DownloadData(url);

                for (int i = 0; i < Twitch.TwitchPinkScreenRetryAttempts; i++)
                {
                    byte[] hash;
                    using (var sha256 = System.Security.Cryptography.SHA256.Create())
                    {
                        hash = sha256.ComputeHash(image);
                    }
                    if (hash.SequenceEqual(Twitch.TwitchPinkScreenChecksum))
                    {
                        //pink screen detected. Lets sleep for X seconds and try again. 
                        Console.WriteLine($"{DateTime.Now.ToString("HH:MM:ss")} Twitter     Detected Pink Screen for {url}, trying again in {Twitch.TwitchPinkScreenRetryDelay}, attempt {i+1} out of {Twitch.TwitchPinkScreenRetryAttempts}" );
                        await Task.Delay(Twitch.TwitchPinkScreenRetryDelay);
                        image = webClient.DownloadData(url);
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
