using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ChallengerBot
{
    public static class ChallengerConfig
    {
        private const string SettingFile = "ChallengerBot.ini";

        public static string GamePath;
        public static bool GameConfigReplace;
        public static int MaxBots;
        public static int MaxLevel;
        public static string PlayHero;
        public static bool XPBoost;
        public static string Region;
        public static bool CreatePremade;

        public static List<int> AID = new List<int>(new int[] { 32, 33, 25, 52 });

        public static void Init()
        {
            if (!File.Exists(SettingFile))
            {
                var BotConfig = new JObject(
                    new JProperty("GamePath", @"C:\Game\Location"),
                    new JProperty("MaxBots", "1"),
                    new JProperty("MaxLevel", "31"),
                    new JProperty("Region", "EUW"),
                    new JProperty("AutoBoost", "false"));

                using (StreamWriter file = File.CreateText(SettingFile))
                using (JsonTextWriter writer = new JsonTextWriter(file))
                {
                    writer.Formatting = Formatting.Indented;
                    BotConfig.WriteTo(writer);
                }

                Init();
            }
            else
            {
                using (StreamReader reader = File.OpenText(SettingFile))
                {
                    JObject settings = (JObject)JToken.ReadFrom(new JsonTextReader(reader));
                    GamePath = (string) settings["GamePath"];
                    MaxBots = (int)settings["MaxBots"];
                    MaxLevel = (int)settings["MaxLevel"];
                    XPBoost = (bool)settings["AutoBoost"];
                    Region = (string)settings["Region"];
                }
            }

            Core.ClientVersion = Controller.GetCurrentVersion(GamePath);
        }
    }
}