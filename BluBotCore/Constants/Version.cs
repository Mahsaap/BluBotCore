using BluBotCore.Global;

namespace BluBotCore
{
    public class Version
    {
        public static string Major { get { return "1"; } }
        public static string Minor { get { return "15"; } }
        public static string Build { get { return BuildType.WYK.Value; } }
    }
}
