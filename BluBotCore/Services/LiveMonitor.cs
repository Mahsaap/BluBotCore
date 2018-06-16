using BluBotCore.Other;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using Tweetinvi;

using TwitchLib.Api;
using TwitchLib.Api.Services;
using TwitchLib.Api.Models.v5.Teams;
using TwitchLib.Api.Models.v5.Channels;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;
using System.Net;
using TwitchLib.Api.Exceptions;
using Tweetinvi.Parameters;
using System.Collections.Concurrent;

namespace BluBotCore.Services
{
    public class LiveMonitor
    {
        #region Private Variables
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _service;

        private static List<string> chansName = new List<string>();
        private static List<string> chansID = new List<string>();

        public static Dictionary<string, ulong> sepServerList = new Dictionary<string, ulong>();
        private static ConcurrentDictionary<string, Tuple<RestUserMessage, string, string>> sepliveEmbeds = new ConcurrentDictionary<string, Tuple<RestUserMessage, string, string>>();

        private static ConcurrentDictionary<string, Tuple<RestUserMessage,string,string>> liveEmbeds = new ConcurrentDictionary<string, Tuple<RestUserMessage,string,string>>();

        private static DateTime onlineTime;
        private static string twitterURL = "";

        #endregion

        #region Public Properties

        public LiveStreamMonitor Monitor { get; private set; }
        public TwitchAPI API { get; private set; }
        public List<String> ChansName { get => chansName; }
        public List<String> ChansID { get => chansID; }
        #endregion

        public LiveMonitor(IServiceProvider service, DiscordSocketClient client, CommandService commands)
        {
            _client = client;
            _service = service;
            _commands = commands;

            Task.Run(() => ConfigLiveMonitorAsync().Wait());
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
                        var mahsaap = _client.GetUser(88798728948809728) as IUser;
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

                Monitor = new LiveStreamMonitor(API, 60, invokeEventsOnStart: false);

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

        private void _monitor_OnStreamOnline(object sender, OnStreamOnlineArgs e)
        {
            Task.Delay(250);
            string url = @"https://www.twitch.tv/" + e.Stream.Channel.Name;
            EmbedBuilder eb = SetupLiveEmbed($":link: {e.Stream.Channel.DisplayName}", $"{e.Stream.Channel.Status}", $"{e.Stream.Channel.Game}",
                e.Stream.Preview.Medium + Guid.NewGuid().ToString(), e.Stream.Channel.Logo, url);

            string time = DateTime.Now.ToString("HH:MM:ss");
            Console.WriteLine($"{time} Monitor     {e.Stream.Channel.DisplayName} is live playing {e.Stream.Game}");

            string twitterTag = FindTwitterTag(e.Stream.Channel.DisplayName);

            Task.Run(() =>
                TweetMessageAsync($"{e.Stream.Channel.DisplayName} is live playing {e.Stream.Game}! {e.Stream.Channel.Url} {twitterTag}#WYKTV", e.Stream.Preview.Medium + Guid.NewGuid().ToString(), e.Stream.Channel.Name.ToLower())
            ).Wait();

            Task.Run(() =>
                SetupEmbedMessageAsync(eb, e, null, twitterURL)
            ).Wait();

            twitterURL = "";
        }

        private void _monitor_OnStreamUpdate(object sender, OnStreamUpdateArgs e)
      {
            if (liveEmbeds.ContainsKey(e.ChannelId))
            {
                if (_client.ConnectionState == ConnectionState.Connected)
                {
                    Task.Run(() =>
                    OnStreamUpdateAsync(e).Wait()
                    );
                }
            }
            /*else
            {
                Task.Delay(250);

                string url = @"https://www.twitch.tv/" + e.Stream.Channel.Name;
                EmbedBuilder eb = SetupLiveEmbed($":link: {e.Stream.Channel.DisplayName}", $"{e.Stream.Channel.Status}", $"{e.Stream.Channel.Game}",
                    e.Stream.Preview.Medium + Guid.NewGuid().ToString(), e.Stream.Channel.Logo, url);

                string time = DateTime.Now.ToString("HH:MM:ss");
                Console.WriteLine($"{time} Monitor     {e.Stream.Channel.DisplayName} is live playing {e.Stream.Game}");

                string twitterTag = FindTwitterTag(e.Stream.Channel.DisplayName);

                Task.Run(() =>
                    SetupEmbedMessageAsync(eb, null, e.Stream, twitterURL)
                ).Wait();

                twitterURL = "";
            }*/
        }

        private async Task OnStreamUpdateAsync(OnStreamUpdateArgs e)
        {
            if (Setup.DiscordAnnounceChannel == 0) return;
            var msg = liveEmbeds[e.ChannelId];
            if (msg.Item2 != e.Stream.Channel.Status || msg.Item3 != e.Stream.Channel.Game)
            {
                EmbedBuilder eb = SetupLiveEmbed($":link: {e.Stream.Channel.DisplayName}", $"{e.Stream.Channel.Status}", $"{e.Stream.Channel.Game}",
                    e.Stream.Preview.Medium + Guid.NewGuid().ToString(), e.Stream.Channel.Logo, @"https://www.twitch.tv/" + e.Stream.Channel.Name);

                await UpdateNotificationAsync(eb, liveEmbeds, e);
                await Task.Delay(500);
                await UpdateNotificationAsync(eb, sepliveEmbeds, e);

                string time = DateTime.Now.ToString("HH:MM:ss");
                Console.WriteLine($"{time} Monitor     Stream {e.Channel} updated");
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

        private void _monitor_OnStreamOffline(object sender, OnStreamOfflineArgs e)
        {
            string time = DateTime.Now.ToString("HH:MM:ss");
            Console.WriteLine($"{time} Monitor     {e.Channel} is offline");
            Task.Run(() =>
                StreamOfflineAsync(liveEmbeds, e, 250)
            );
            Task.Run(() =>
                StreamOfflineAsync(sepliveEmbeds, e, 500)
            );
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

        private void _monitor_OnStreamMonitorStarted(object sender, OnStreamMonitorStartedArgs e)
        {
            onlineTime = DateTime.Now;
            twitterURL = "";
            string time = DateTime.Now.ToString("HH:MM:ss");
            Console.WriteLine($"{time} Monitor     Started");
            liveEmbeds.Clear();
            sepliveEmbeds.Clear();

            if (_client.ConnectionState == ConnectionState.Connected)
            {
                Task.Run(() =>
                    StartUpdateLiveMessagesChannelAsync().Wait()
                    );
            }
        }

        private async Task StartUpdateLiveMessagesChannelAsync()
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

            foreach (var e in Monitor.CurrentLiveStreams)
            {
                await Task.Delay(250);
                EmbedBuilder eb = SetupLiveEmbed($":link: {e.Channel.DisplayName}", $"{e.Channel.Status}", $"{e.Channel.Game}",
                e.Preview.Medium + Guid.NewGuid().ToString(), e.Channel.Logo, @"https://www.twitch.tv/" + e.Channel.Name);

                string time = DateTime.Now.ToString("HH:MM:ss");
                Console.WriteLine($"{time} Monitor     {e.Channel.DisplayName} is live playing {e.Game}");
                await Task.Delay(1000);
                await SetupEmbedMessageAsync(eb, null, e, "");
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
            Team team = await API.Teams.v5.GetTeamAsync("wyktv");

            foreach (Channel user in team.Users)
            {
                chansName.Add(user.Name);
                chansID.Add(user.Id);
            }
            Monitor.SetStreamsByUserId(chansID);
        }

        public async Task UpdateMonitorAsync()
        {
            Monitor.StopService();
            chansName.Clear();
            chansID.Clear();
            await SetCastersAsync();
            Monitor.StartService();
        }


        private async Task SetupEmbedMessageAsync(EmbedBuilder eb, OnStreamOnlineArgs e, TwitchLib.Api.Models.v5.Streams.Stream s, string twitterUrl)
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
                if (onlineTime.AddSeconds(30) <= DateTime.Now) here = "@here ";
                if (twitterUrl.Length > 1) here += $"\nTwitter (*<{twitterUrl}>*)"; else here += " ";
                here += $"\nTwitch (*{twitchURL}*)";
                here = here.Insert(0, $"**{channelName} is live!** ");

                await SendEmbedAsync(Setup.DiscordAnnounceChannel, eb, liveEmbeds, here, channelID, channelName, status, game);

                if (sepServerList.ContainsKey(channelName.ToLower()))
                {
                    await Task.Delay(500);
                    await SendEmbedAsync(sepServerList[channelName.ToLower()], eb, sepliveEmbeds, here, channelID, channelName, status, game);
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


        private async Task TweetMessageAsync(string text, string url, string channel)
        {
            try
            {
                WebClient webClient = new WebClient();
                byte[] image = webClient.DownloadData(url);
                var publishOptions = new PublishTweetOptionalParameters();
                publishOptions.MediaBinaries.Add(image);
                var twitterObject = await TweetAsync.PublishTweet(text, publishOptions);
                twitterURL = twitterObject.Url;

            }
            catch (Exception ex)
            {
                var mahsaap = _client.GetUser(88798728948809728) as IUser;
                await mahsaap.SendMessageAsync(ex.Message + "\n" + ex.StackTrace);
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
