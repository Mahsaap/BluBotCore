using System;

namespace BluBotCore
{
    static class Globals
    {
        public static string CurrentTime { get { return DateTime.Now.ToString("HH:mm:ss"); } }

        public static string NullEmptyCheck(string entry)
        {
            if (!String.IsNullOrEmpty(entry))
            {
                return entry;
            }
            else
                return ".";
        }
    }
}
