using System;

namespace BluBotCore
{
    static class Globals
    {
        public static string CurrentTime { get { return DateTime.Now.ToString("HH:mm:ss"); } }

        public static string NullEmptyCheck(string entry)
        {
            if (!string.IsNullOrEmpty(entry))
            {
                return entry;
            }
            else
                return ".";
        }

        public static string EditPreviewURL(string url)
        {
            if (NullEmptyCheck(url) == ".")
            {
                return ".";
            }
            else
            {
                string thumburl = url;
                thumburl = thumburl.Replace("{width}", "320");
                thumburl = thumburl.Replace("{height}", "180");
                return thumburl;
            }
        }
    }
}