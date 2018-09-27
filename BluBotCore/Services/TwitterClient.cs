using BluBotCore.Other;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;

namespace BluBotCore.Services
{
    public class TwitterClient
    {
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _service;
        IAuthenticatedUser _authenticatedUser;

        public TwitterClient(IServiceProvider service, DiscordSocketClient client)
        {
            _client = client;
            _service = service;

            TwitterStartAsync().GetAwaiter().GetResult();
        }

        private async Task TwitterStartAsync()
        {
            Auth.SetUserCredentials(AES.Decrypt(Cred.TwitterConsumerKey), AES.Decrypt(Cred.TwitterConsumerSecret)
                , AES.Decrypt(Cred.TwitterAccessKey), AES.Decrypt(Cred.TwitterAccessSecret));
            _authenticatedUser = User.GetAuthenticatedUser();

            string time = DateTime.Now.ToString("HH:MM:ss");
            Console.WriteLine($"{time} Twitter     Connected to {_authenticatedUser}");
            await Task.CompletedTask;
        }
    }
}
