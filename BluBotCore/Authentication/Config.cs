using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BluBotCore.Authentication
{
    public static class Config
    {
        public static readonly string TwitchClientId = "<TWITCH_CLIENT_ID>";
        public static readonly string TwitchRedirectUri = "http://localhost:8080/redirect/";
        public static readonly string TwitchClientSecret = "<TWITCH_CLIENT_SECRET>";
    }
}
