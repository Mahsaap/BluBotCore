namespace BluBotCore
{
    public class Twitch
    {
        public static byte[] TwitchPinkScreenChecksum {
            get { return new byte[]
            {
                11, 241, 144, 174,
                218, 192, 175, 31,
                120, 108, 52, 36,
                55, 174, 200, 134,
                12, 8, 223, 245,
                175, 184, 76, 16,
                140, 201, 39, 57,
                123, 39, 23, 78
                };
            }
        }
        public static int TwitchPinkScreenRetryAttempts { get { return 3; } }
        public static int TwitchPinkScreenRetryDelay { get { return 15000; } } //ms
    }
}