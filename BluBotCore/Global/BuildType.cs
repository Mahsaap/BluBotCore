namespace BluBotCore.Global
{
    public class BuildType
    {
        private BuildType(string value) { Value = value; }

        public string Value { get; set; }

        public static BuildType WYK { get { return new BuildType("WYK"); } }
        public static BuildType OBG { get { return new BuildType("OBG"); } }
    }
}