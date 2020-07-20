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
using TwitchLib.Api;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.V5.Models.Channels;
using TwitchLib.Api.V5.Models.Teams;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;
using BluBotCore.Global;

namespace BluBotCore.Services
{
    public class LiveMonitor
    {
        #region Private Variables

            /// <summary> Discord Client instance </summary>
            private readonly DiscordSocketClient _client;

            /// <summary> Time the bot comes online. This is used to not tweet / @everyone current online streamers on load. </summary>
            private static DateTime _botOnlineTime;

            static DateTime LastTeamCheck;
            /// <summary> Dictionary of all the live discord messages. Concurrent since this can be added/removed from at anytime. </summary>
            public static ConcurrentDictionary<string, Tuple<RestUserMessage,string,string,int>> _liveEmbeds = new ConcurrentDictionary<string, Tuple<RestUserMessage,string,string,int>>();

        #endregion

        #region Properties

            /// <summary> Live Monitor Instance. </summary>
            public LiveStreamMonitorService Monitor { get; private set; }

            /// <summary> Twitch API Instance. </summary>
            public TwitchAPI API { get; private set; }

            /// <summary> Dictionary of all the current monitored channels. </summary>
            public static Dictionary<String, String> MonitoredChannels { get; } = new Dictionary<string, string>();

        #endregion

        /// <summary> Constructor. Injects Dicord Client instance. Starts Live Monitor configuration. </summary>
        /// <param name="client"></param>
        public LiveMonitor(DiscordSocketClient client)
        {
            _client = client;

            Task.Run(() => ConfigLiveMonitorAsync());
        }

        /// <summary> Live Monitor configuration. </summary>
        private async Task ConfigLiveMonitorAsync()
        {
            // Ensure Discord is connected before config continues. Loop every 2 seconds till online.
            while (_client.ConnectionState != ConnectionState.Connected)
            {
                await Task.Delay(2000);
            }
            try
            {
                API = new TwitchAPI();
                try
                {
                    // Set Credentials in Twitch API Config.
                    API.Settings.ClientId = AES.Decrypt(Cred.TwitchAPIID);
                    API.Settings.Secret = AES.Decrypt(Cred.TwitchAPISecret);
                }
                catch (Exception ex)
                {
                    // Token Expired Refresh Sequence.
                    if (ex is TokenExpiredException)
                    {
                        var mahsaap = (_client.GetUser(DiscordIDs.Mahsaap) as IUser);
                        await mahsaap.SendMessageAsync("TwitchLib token has expired.");

                        // Refresh Token
                        var token = await API.V5.Auth.RefreshAuthTokenAsync(
                            AES.Decrypt(Cred.TwitchAPIRefreshToken), AES.Decrypt(Cred.TwitchAPISecret), AES.Decrypt(Cred.TwitchAPIID));
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
                        Cred.TwitchAPISecret = AES.Encrypt(token.AccessToken);
                        tmpList[3] = AES.Encrypt(token.RefreshToken);
                        Cred.TwitchAPIRefreshToken = AES.Encrypt(token.RefreshToken);

                        // Save (overwrite) the file.
                        File.WriteAllLines("init.txt", tmpList);

                        await mahsaap.SendMessageAsync($"TwitchLib keys have been updated in file. Expires in {token.ExpiresIn}.");

                        // Set Credentials in Twitch API Config.
                        API.Settings.ClientId = AES.Decrypt(Cred.TwitchAPIID);
                        API.Settings.AccessToken = AES.Decrypt(Cred.TwitchAPISecret);
                        Console.WriteLine($"{Globals.CurrentTime} Monitor     Tokens have been refreshed and updated!");
                    }
                }

                Monitor = new LiveStreamMonitorService(API, 300);

                Console.WriteLine($"{Globals.CurrentTime} Monitor     Instance Created");

                await SetCastersAsync();

                // Events
                Monitor.OnStreamOnline += Monitor_OnStreamOnlineAsync;
                Monitor.OnStreamOffline += Monitor_OnStreamOfflineAsync;
                Monitor.OnStreamUpdate += Monitor_OnStreamUpdateAsync;
                Monitor.OnServiceStarted += Monitor_OnServiceStartedAsync;
                Monitor.OnChannelsSet += Monitor_OnChannelsSet;

                Monitor.Start();

                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
            }
        }

        private async void Monitor_OnStreamOnlineAsync(object sender, OnStreamOnlineArgs e)
        {
            try
            {
                var ee = await API.V5.Streams.GetStreamByUserAsync(e.Channel);

                if (Version.Build == BuildType.OBG.Value)
                {
                    if (_liveEmbeds.ContainsKey(e.Channel) && _client.ConnectionState == ConnectionState.Connected)
                    {
                        RestUserMessage embed = _liveEmbeds[e.Channel].Item1;
                        await embed.DeleteAsync();
                        _ = _liveEmbeds.TryRemove(e.Channel, out _);
                    }
                }
                if (!_liveEmbeds.ContainsKey(ee.Stream.Channel.Id) && _client.ConnectionState == ConnectionState.Connected)
                {
                    string url = @"https://www.twitch.tv/" + ee.Stream.Channel.Name;
                    EmbedBuilder eb = SetupLiveEmbed($":link: {ee.Stream.Channel.DisplayName}", ee.Stream.Channel.Status, ee.Stream.Channel.Game,
                        ee.Stream.Preview.Medium + Guid.NewGuid(), ee.Stream.Channel.Logo, url, ee.Stream.Viewers);

                    Console.WriteLine($"{Globals.CurrentTime} Monitor     {ee.Stream.Channel.DisplayName} is live playing {ee.Stream.Game}");

                    await Task.Delay(1000);
                    await SetupEmbedMessageAsync(eb, ee, null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private async void Monitor_OnStreamOfflineAsync(object sender, OnStreamOfflineArgs e)
        {
            try
            {
                var ee = await API.V5.Channels.GetChannelByIDAsync(e.Channel);
                Console.WriteLine($"{Globals.CurrentTime} Monitor     {ee.DisplayName} is offline");

                if (_liveEmbeds.ContainsKey(e.Channel) && _client.ConnectionState == ConnectionState.Connected)
                {
                    if (Version.Build == BuildType.OBG.Value)
                    {
                        RestUserMessage embed = _liveEmbeds[e.Channel].Item1;
                        string text = "**No, OverBoredGaming is not live!**\n" +
                            "But you can check out the rest of the WYK Team!\n" +
                            "<https://www.twitch.tv/team/wyktv>";
                        await embed.ModifyAsync(x => x.Content = text);
                        await Task.Delay(250);
                        await embed.ModifyAsync(x => x.Embed = null);
                    }
                    if (Version.Build == BuildType.WYK.Value)
                    {
                        await Task.Delay(250);
                        RestUserMessage embed = _liveEmbeds[e.Channel].Item1;
                        await embed.DeleteAsync();
                        _ = _liveEmbeds.TryRemove(e.Channel, out _);
                        // Console.WriteLine($"{Global.CurrentTime} Monitor     TryParse OutResult: {outResult}");
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private async void Monitor_OnStreamUpdateAsync(object sender, OnStreamUpdateArgs e)
        {
            if (Version.Build == BuildType.WYK.Value)
            {
                if (DateTime.Now > LastTeamCheck.AddDays(1))
                {
                    try
                    {
                        var teamTemp = await API.V5.Teams.GetTeamAsync("wyktv");
                        // Check Team Count
                        if (teamTemp.Users.Length != MonitoredChannels.Count)
                        {
                            await UpdateMonitorAsync();
                            return;
                        }
                        else
                        {
                            // Check Name Change
                            int count = 0;
                            var result = MonitoredChannels.Where(p => teamTemp.Users.All(p2 => p2.Id != p.Value));
                            foreach (var r in result)
                            {
                                count++;
                            }
                            if (count > 0)
                            {
                                await UpdateMonitorAsync();
                                return;
                            }
                        }
                        LastTeamCheck = DateTime.Now;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }

            var ee = await API.V5.Streams.GetStreamByUserAsync(e.Channel);

            if (_liveEmbeds.ContainsKey(e.Channel))
            {
                if (_client.ConnectionState == ConnectionState.Connected)
                {
                    if (Setup.DiscordAnnounceChannel == 0) return;
                    if (Version.Build == BuildType.OBG.Value)
                    {
                        if (_liveEmbeds[e.Channel].Item1.Embeds.Count == 0) return;
                    }
                    var msg = _liveEmbeds[e.Channel];
                    if (msg.Item2 != ee.Stream.Channel.Status || msg.Item3 != ee.Stream.Channel.Game || msg.Item4 != ee.Stream.Viewers)
                    {
                        EmbedBuilder eb = SetupLiveEmbed($":link: {ee.Stream.Channel.DisplayName}", $"{ee.Stream.Channel.Status}", $"{ee.Stream.Channel.Game}",
                            ee.Stream.Preview.Medium + Guid.NewGuid().ToString(), ee.Stream.Channel.Logo, @"https://www.twitch.tv/" + ee.Stream.Channel.Name, ee.Stream.Viewers);

                        await UpdateNotificationAsync(eb, _liveEmbeds, e);

                        Console.WriteLine($"{Globals.CurrentTime} Monitor     Stream {ee.Stream.Channel.DisplayName} updated");
                        await Task.Delay(500);
                    }
                }
            }
        }

        private async void Monitor_OnServiceStartedAsync(object sender, TwitchLib.Api.Services.Events.OnServiceStartedArgs e)
        {
            _botOnlineTime = DateTime.Now;

            Console.WriteLine($"{Globals.CurrentTime} Monitor     Started");
            _liveEmbeds.Clear();
            try
            {
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
                        EmbedBuilder eb = SetupLiveEmbed($":link: {xx.Stream.Channel.DisplayName}", xx.Stream.Channel.Status, xx.Stream.Channel.Game,
                        xx.Stream.Preview.Medium + Guid.NewGuid().ToString(), xx.Stream.Channel.Logo, @"https://www.twitch.tv/" + xx.Stream.Channel.Name, xx.Stream.Viewers);

                        Console.WriteLine($"{Globals.CurrentTime} Monitor     {xx.Stream.Channel.DisplayName} is live playing {xx.Stream.Game}");
                        await Task.Delay(1000);
                        await SetupEmbedMessageAsync(eb, null, xx.Stream);
                    }

                    if (Version.Build == BuildType.OBG.Value && livestreamers.Streams.Length == 0)
                    {
                        var id = (await API.Helix.Users.GetUsersAsync(logins: new List<string> { "overboredgaming" })).Users[0].Id;
                        string text = "**No, OverBoredGaming is not live!**\n" +
                            "But you can check out the rest of the WYK Team!\n" +
                            "<https://www.twitch.tv/team/wyktv>";
                        RestUserMessage msg = await chan.SendMessageAsync(text);
                        _liveEmbeds.TryAdd(id, new Tuple<RestUserMessage, string, string, int>(msg, "", "", 0));
                        await Task.CompletedTask;
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void Monitor_OnChannelsSet(object sender, TwitchLib.Api.Services.Events.OnChannelsSetArgs e)
        {
            Console.WriteLine($"{Globals.CurrentTime} Monitor     Streams Set!");
        }

        private async Task UpdateNotificationAsync(EmbedBuilder eb, ConcurrentDictionary<string, Tuple<RestUserMessage, string, string, int>> lst, OnStreamUpdateArgs e)
        {
            try
            {
                var ee = await API.V5.Streams.GetStreamByUserAsync(e.Channel);
                if (lst.ContainsKey(e.Channel))
                {
                    var msg = lst[e.Channel];
                    await msg.Item1.ModifyAsync(x => x.Embed = eb.Build());
                    lst[e.Channel] = new Tuple<RestUserMessage, string, string, int>(msg.Item1, ee.Stream.Channel.Status, ee.Stream.Channel.Game, ee.Stream.Viewers);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private EmbedBuilder SetupLiveEmbed(string title, string description, string game, string image, string thumbnail, string url, int vCount)
        {

            title = Globals.NullEmptyCheck(title);
            description = Globals.NullEmptyCheck(description);
            game = Globals.NullEmptyCheck(game);

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
                x.IsInline = true;
            });
            eb.AddField(x =>
            {
                x.Name = $"Viewer Count";
                x.Value = vCount;
                x.IsInline = true;
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

        private async Task SetCastersAsync()
        {
            try
            {
                LastTeamCheck = DateTime.Now;
                if (Version.Build == BuildType.WYK.Value)
                {
                    Team team = await API.V5.Teams.GetTeamAsync("wyktv");

                    foreach (Channel user in team.Users)
                    {
                        MonitoredChannels.Add(user.DisplayName, user.Id);
                    }
                }
                else if (Version.Build == BuildType.OBG.Value)
                {
                    var chan = await API.Helix.Users.GetUsersAsync(logins: new List<string> { "overboredgaming" });
                    MonitoredChannels.Add(chan.Users[0].DisplayName, chan.Users[0].Id);
                }
                Monitor.SetChannelsById(MonitoredChannels.Values.ToList());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public async Task<bool> UpdateMonitorAsync(string channel = null)
        {
            if (channel == null) {
                Monitor.Stop();
                MonitoredChannels.Clear();
                await SetCastersAsync();
                Monitor.Start();
                return true;
            }
            else
            {
                try
                {
                    var user = await API.V5.Users.GetUserByNameAsync(channel);
                    string channelID = user.Matches[0].Id;

                    var ee = await API.V5.Streams.GetStreamByUserAsync(channelID);
                    if (_liveEmbeds.ContainsKey(channelID))
                    {
                        if (_client.ConnectionState == ConnectionState.Connected)
                        {
                            if (Setup.DiscordAnnounceChannel == 0) return false;
                            var msg = _liveEmbeds[channelID];

                            EmbedBuilder eb = SetupLiveEmbed($":link: {ee.Stream.Channel.DisplayName}", ee.Stream.Channel.Status, ee.Stream.Channel.Game,
                                ee.Stream.Preview.Medium + Guid.NewGuid().ToString(), ee.Stream.Channel.Logo, @"https://www.twitch.tv/" + ee.Stream.Channel.Name, ee.Stream.Viewers);

                            await msg.Item1.ModifyAsync(x => x.Embed = eb.Build());
                            _liveEmbeds[channelID] = new Tuple<RestUserMessage, string, string,int>(msg.Item1, ee.Stream.Channel.Status, ee.Stream.Channel.Game, ee.Stream.Viewers);

                            Console.WriteLine($"{Globals.CurrentTime} Monitor     Stream {ee.Stream.Channel.DisplayName} updated");
                            return true;
                        }
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            }
        }

        public async Task<bool> RemoveLiveEmbedAsync(string channel)
        {
            try
            {
                var user = await API.V5.Users.GetUserByNameAsync(channel);
                string channelID = user.Matches[0].Id;
                var ee = await API.V5.Channels.GetChannelByIDAsync(channelID);
                Console.WriteLine($"{Globals.CurrentTime} Monitor     {ee.DisplayName} was removed manually.");

                if (_liveEmbeds.ContainsKey(channelID))
                {
                    await Task.Delay(250);
                    RestUserMessage embed = _liveEmbeds[channelID].Item1;
                    if (_client.ConnectionState == ConnectionState.Connected)
                        await embed.DeleteAsync();
                    _liveEmbeds.TryRemove(channelID, out Tuple<RestUserMessage, string, string, int> outResult);
                    Console.WriteLine($"{Globals.CurrentTime} Monitor     TryParse OutResult: {outResult}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }

        }

        private async Task SetupEmbedMessageAsync(EmbedBuilder eb, TwitchLib.Api.V5.Models.Streams.StreamByUser e, TwitchLib.Api.V5.Models.Streams.Stream s)
        {
            try
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
                int vCount = ee?.Stream.Viewers ?? s.Viewers;

                if (Setup.DiscordAnnounceChannel == 0) return;
                if (_client.ConnectionState == ConnectionState.Connected)
                {
                    string here = "";
                    if (Version.Build == BuildType.OBG.Value)
                    {
                        here += $"**Yes, {channelName} is live!**\n" +
                            $"Twitch(*{twitchURL}*)";
                    }
                    else
                    {
                        if (_botOnlineTime.AddSeconds(30) <= DateTime.Now) here = "@here ";
                        here += $"\nTwitch (*{twitchURL}*)";
                        here = here.Insert(0, $"**{channelName} is live!** ");
                    }

                    await SendEmbedAsync(Setup.DiscordAnnounceChannel, eb, here, channelID, status, game, vCount);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private async Task SendEmbedAsync(ulong id, EmbedBuilder eb, string here, string channelID, string status, string game, int vCount)
        {
            var chan = _client.GetChannel(id) as SocketTextChannel;
            RestUserMessage msg = await chan.SendMessageAsync(here, embed: eb.Build());
            if (_liveEmbeds.ContainsKey(channelID)) _liveEmbeds[channelID] = new Tuple<RestUserMessage, string, string, int>(msg, status, game, vCount);
            else _liveEmbeds.TryAdd(channelID, new Tuple<RestUserMessage, string, string, int>(msg, status, game, vCount));
            await Task.CompletedTask;
        }

        //private async Task<string> ImageCheck(string url)
        //{
        //    WebClient webClient = new WebClient();
        //    try
        //    {
        //        byte[] image = webClient.DownloadData(url);

        //        for (int i = 0; i < Twitch.TwitchPinkScreenRetryAttempts; i++)
        //        {
        //            byte[] hash;
        //            using (var sha256 = System.Security.Cryptography.SHA256.Create())
        //            {
        //                hash = sha256.ComputeHash(image);
        //            }
        //            if (hash.SequenceEqual(Twitch.TwitchPinkScreenChecksum))
        //            {
        //                //pink screen detected. Lets sleep for X seconds and try again.
        //                Console.WriteLine($"{DateTime.Now:HH:MM:ss} Twitch       Detected Pink Screen for {url}, trying again in {Twitch.TwitchPinkScreenRetryDelay}, attempt {i + 1} out of {Twitch.TwitchPinkScreenRetryAttempts}");
        //                await Task.Delay(Twitch.TwitchPinkScreenRetryDelay);
        //                image = webClient.DownloadData(url);
        //                image = webClient.DownloadData(url);
        //            }
        //            else
        //            {
        //                //not a pink screen. Break out of loop
        //                return url;
        //            }
        //        }
        //        return url;
        //    }
        //    catch (Exception ex)
        //    {
        //        var mahsaap = _client.GetUser(DiscordIDs.Mahsaap) as IUser;
        //        await mahsaap.SendMessageAsync(ex.Message + "\n" + ex.StackTrace);
        //        //cannot return null - code checks for length > 1
        //        return "";
        //    }
        //    finally
        //    {
        //        webClient.Dispose();
        //    }
        //}
    }
}
