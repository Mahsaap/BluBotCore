using BluBotCore.Other;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;
using BluBotCore.Global;

namespace BluBotCore.Services
{
    public class LiveMonitor
    {

        private readonly DiscordSocketClient _client;
        private static DateTime _botOnlineTime;
        static DateTime LastTeamCheck;
        private static string teamBanner;
        private static readonly ConcurrentDictionary<string, Tuple<RestUserMessage, string, string, int>> concurrentDictionary = new();
        private static ConcurrentDictionary<string, Tuple<RestUserMessage, string, string, int>> _liveEmbeds = concurrentDictionary;
        public LiveStreamMonitorService Monitor { get; private set; }
        public TwitchAPI API { get; private set; }
        public static Dictionary<String, String> MonitoredChannels { get; } = new Dictionary<string, string>();

        public LiveMonitor(DiscordSocketClient client)
        {
            _client = client;
            Task.Run(() => ConfigLiveMonitorAsync());
        }

        private async Task ConfigLiveMonitorAsync()
        {
            while (_client.ConnectionState != ConnectionState.Connected)
            {
                await Task.Delay(2000);
            }
            try
            {
                API = new TwitchAPI();
                try
                {
                    API.Settings.ClientId = Cred.TwitchAPIID;
                    API.Settings.Secret = Cred.TwitchAPISecret;
                }
                catch (Exception ex)
                {
                    // Token Expired Refresh Sequence.
                    if (ex is TokenExpiredException)
                    {
                        var mahsaap = (_client.GetUser(DiscordIDs.Mahsaap) as IUser);
                        await mahsaap.SendMessageAsync("TwitchLib token has expired.");

                        var token = await API.Auth.RefreshAuthTokenAsync(
                            AES.Decrypt(Cred.TwitchAPIRefreshToken), AES.Decrypt(Cred.TwitchAPISecret), AES.Decrypt(Cred.TwitchAPIID));
                        await mahsaap.SendMessageAsync("TwitchLib token has been refreshed.");

                        List<string> tmpList = new();
                        using (StreamReader file = new("init.txt"))
                        {
                            string dataOld;
                            while ((dataOld = file.ReadLine()) != null)
                                tmpList.Add(dataOld);
                            file.Close();
                        }

                        tmpList[2] = token.AccessToken;
                        Cred.TwitchAPISecret = token.AccessToken;
                        tmpList[3] = token.RefreshToken;
                        Cred.TwitchAPIRefreshToken = token.RefreshToken;

                        File.WriteAllLines("init.txt", tmpList);

                        await mahsaap.SendMessageAsync($"TwitchLib keys have been updated in file. Expires in {token.ExpiresIn}.");

                        API.Settings.ClientId = Cred.TwitchAPIID;
                        API.Settings.AccessToken = Cred.TwitchAPISecret;
                        Console.WriteLine($"{Globals.CurrentTime} Monitor     Tokens have been refreshed and updated!");
                    }
                }

                Monitor = new LiveStreamMonitorService(API, 300);

                Console.WriteLine($"{Globals.CurrentTime} Monitor     Instance Created");
                await SetCastersAsync();

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
                var s = (await API.Helix.Streams.GetStreamsAsync(userIds: new List<string>{ e.Channel })).Streams[0];
                if (Version.Build == BuildType.OBG.Value)
                {
                    if (_liveEmbeds.ContainsKey(e.Channel) && _client.ConnectionState == ConnectionState.Connected)
                    {
                        RestUserMessage embed = _liveEmbeds[e.Channel].Item1;
                        if (embed.Content.Contains("**No, OverBoredGaming is not live!**\n" +
                            "But you can check out the rest of the WYK Team!\n" +
                            "<https://www.twitch.tv/team/wyktv>")){
                            await embed.DeleteAsync();
                            _ = _liveEmbeds.TryRemove(e.Channel, out _);
                        }
                    }
                }

                if (!_liveEmbeds.ContainsKey(s.UserId) && _client.ConnectionState == ConnectionState.Connected)
                {
                    string thumburl = Globals.EditPreviewURL(s.ThumbnailUrl);
                    var user = (await API.Helix.Users.GetUsersAsync(ids: new List<string> { $"{s.UserId}" })).Users[0];
                    string url = @"https://www.twitch.tv/" + s.UserName;
                    EmbedBuilder eb = SetupLiveEmbed($":link: {s.UserName}", s.Title, s.GameName,
                        thumburl + Guid.NewGuid(), user.ProfileImageUrl, url);

                    Console.WriteLine($"{Globals.CurrentTime} Monitor     {s.UserName} is live playing {s.GameName}");

                    await Task.Delay(1000);
                    await SetupEmbedMessageAsync(eb, s);
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
                var s = (await API.Helix.Users.GetUsersAsync(ids: new List<string> { e.Channel })).Users[0];
                Console.WriteLine($"{Globals.CurrentTime} Monitor     {s.DisplayName} is offline");

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
                        await embed.ModifyAsync(x => x.Flags = MessageFlags.SuppressEmbeds);
                    }
                    if (Version.Build == BuildType.WYK.Value)
                    {
                        await Task.Delay(250);
                        RestUserMessage embed = _liveEmbeds[e.Channel].Item1;
                        await embed.DeleteAsync();
                        _ = _liveEmbeds.TryRemove(e.Channel, out _);
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
                if (LastTeamCheck.AddDays(1) <= DateTime.Now)
                {
                    try
                    {
                        var teamTemp = (await API.Helix.Teams.GetTeamsAsync(teamName: "wyktv")).Teams[0];
                        //Check Team Count
                        if (teamTemp.Users.Length != MonitoredChannels.Count)
                        {
                            await UpdateMonitorAsync();
                            return;
                        }
                        else
                        {
                            //Check Name Change
                            int count = 0;
                            var result = MonitoredChannels.Where(p => teamTemp.Users.All(p2 => p2.UserId != p.Value));
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

            var s = (await API.Helix.Streams.GetStreamsAsync(userIds: new List<string> { e.Channel })).Streams[0];

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
                    if (msg.Item2 != s.Title || msg.Item3 != s.GameName)
                    {
                        string thumburl = Globals.EditPreviewURL(s.ThumbnailUrl);
                        var user = (await API.Helix.Users.GetUsersAsync(ids: new List<string> { $"{s.UserId}" })).Users[0];
                        EmbedBuilder eb = SetupLiveEmbed($":link: {s.UserName}", $"{s.Title}", $"{s.GameName}",
                            thumburl + Guid.NewGuid(), user.ProfileImageUrl, @"https://www.twitch.tv/" + s.UserName);

                        await UpdateNotificationAsync(eb, _liveEmbeds, e);

                        Console.WriteLine($"{Globals.CurrentTime} Monitor     Stream {s.UserName} updated");
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
                var teamStreams = await API.Helix.Streams.GetStreamsAsync(userIds: Monitor.ChannelsToMonitor);

                if (_client.ConnectionState == ConnectionState.Connected)
                {
                    if (Setup.DiscordAnnounceChannel == 0) return;
                    var chan = _client.GetChannel(Setup.DiscordAnnounceChannel) as SocketTextChannel;

                    var messages = await chan.GetMessagesAsync().FlattenAsync();
                    try
                    {
                        if (messages.Any()) await chan.DeleteMessagesAsync(messages);
                    }
                    catch
                    {
                        foreach (var mes in messages)
                        {
                            await Task.Delay(500);
                            await mes.DeleteAsync();
                        }
                    }

                    if (teamBanner != null)
                    {
                        await chan.SendMessageAsync(teamBanner);
                    }

                    foreach (var s in teamStreams.Streams)
                    {
                        if (s.Type == "live")
                        {
                            var user = (await API.Helix.Users.GetUsersAsync(ids: new List<string> { s.UserId })).Users[0];
                            string thumburl = Globals.EditPreviewURL(s.ThumbnailUrl);
                            EmbedBuilder eb = SetupLiveEmbed($":link: {s.UserName}", s.Title, s.GameName,
                            thumburl + Guid.NewGuid(), user.ProfileImageUrl, @"https://www.twitch.tv/" + s.UserName);

                            Console.WriteLine($"{Globals.CurrentTime} Monitor     {s.UserName} is live playing {s.GameName}");
                            await Task.Delay(2000);
                            await SetupEmbedMessageAsync(eb, s);
                        }
                    }

                    if (Version.Build == BuildType.OBG.Value && teamStreams.Streams.Length == 0)
                    {
                        var id = (await API.Helix.Users.GetUsersAsync(logins: new List<string> { "overboredgaming" })).Users[0];
                        string text = "**No, OverBoredGaming is not live!**\n" +
                            "But you can check out the rest of the WYK Team!\n" +
                            "<https://www.twitch.tv/team/wyktv>";
                        RestUserMessage msg = await chan.SendMessageAsync(text);
                        _liveEmbeds.TryAdd(id.Id, new Tuple<RestUserMessage, string, string, int>(msg, "", "", 0));
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
                var s = (await API.Helix.Streams.GetStreamsAsync(userIds: new List<string> { e.Channel })).Streams[0];
                if (lst.ContainsKey(e.Channel))
                {
                    var msg = lst[e.Channel];
                    await msg.Item1.ModifyAsync(x => x.Embed = eb.Build());
                    lst[e.Channel] = new Tuple<RestUserMessage, string, string, int>(msg.Item1, s.Title, s.GameName, s.ViewerCount);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static EmbedBuilder SetupLiveEmbed(string title, string description, string game, string image, string thumbnail, string url)
        {
            title = Globals.NullEmptyCheck(title);
            description = Globals.NullEmptyCheck(description);
            game = Globals.NullEmptyCheck(game);

            EmbedBuilder eb = new()
            {
                Color = new Color(51, 102, 153),
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

        private async Task SetCastersAsync()
        {
            try
            {
                LastTeamCheck = DateTime.Now;
                if (Version.Build == BuildType.WYK.Value)
                {
                    var team = (await API.Helix.Teams.GetTeamsAsync(teamName: "wyktv")).Teams[0];
                    teamBanner = team.Banner;
                    foreach (var user in team.Users)
                    {
                        MonitoredChannels.Add(user.UserName, user.UserId);
                    }
                }
                else if (Version.Build == BuildType.OBG.Value)
                {
                    var chan = (await API.Helix.Users.GetUsersAsync(logins: new List<string> { "overboredgaming" })).Users[0];
                    MonitoredChannels.Add(chan.DisplayName, chan.Id);
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
                    var s = (await API.Helix.Streams.GetStreamsAsync(userLogins: new List<string> { channel })).Streams[0];
                    if (_liveEmbeds.ContainsKey(channel))
                    {
                        if (_client.ConnectionState == ConnectionState.Connected)
                        {
                            if (Setup.DiscordAnnounceChannel == 0) return false;
                            var msg = _liveEmbeds[s.UserId];

                            string thumburl = Globals.EditPreviewURL(s.ThumbnailUrl);
                            var user = (await API.Helix.Users.GetUsersAsync(ids: new List<string> { $"{s.UserId}" })).Users[0];
                            EmbedBuilder eb = SetupLiveEmbed($":link: {s.UserName}", s.Title, s.GameName,
                                thumburl + Guid.NewGuid().ToString(), user.ProfileImageUrl, @"https://www.twitch.tv/" + s.UserName);

                            await msg.Item1.ModifyAsync(x => x.Embed = eb.Build());
                            _liveEmbeds[s.UserId] = new Tuple<RestUserMessage, string, string,int>(msg.Item1, s.Title, s.GameName, s.ViewerCount);

                            Console.WriteLine($"{Globals.CurrentTime} Monitor     Stream {s.UserName} updated");
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
                var s = (await API.Helix.Streams.GetStreamsAsync(userLogins: new List<string> { channel })).Streams[0];
                Console.WriteLine($"{Globals.CurrentTime} Monitor     {s.UserName} was removed manually.");

                if (_liveEmbeds.ContainsKey(s.UserId))
                {
                    await Task.Delay(250);
                    RestUserMessage embed = _liveEmbeds[s.UserId].Item1;
                    if (_client.ConnectionState == ConnectionState.Connected)
                        await embed.DeleteAsync();
                    _liveEmbeds.TryRemove(s.UserId, out Tuple<RestUserMessage, string, string, int> outResult);
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

        private async Task SetupEmbedMessageAsync(EmbedBuilder eb, TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream s)
        {
            try
            {
                if (Setup.DiscordAnnounceChannel == 0) return;
                if (_client.ConnectionState == ConnectionState.Connected)
                {
                    string here = "";
                    if (Version.Build == BuildType.OBG.Value)
                    {
                        here += $"**Yes, {s.UserName} is live!**\n" +
                            $"Twitch(*https://www.twitch.tv/{s.UserName}*)";
                    }
                    else
                    {
                        if (_botOnlineTime.AddSeconds(30) <= DateTime.Now) here = "@here ";
                        here += $"\nTwitch (*https://www.twitch.tv/{s.UserName}*)";
                        here = here.Insert(0, $"**{s.UserName} is live!** ");
                    }
                    await SendEmbedAsync(Setup.DiscordAnnounceChannel, eb, here, s.UserId, s.Title, s.GameName, s.ViewerCount);
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
