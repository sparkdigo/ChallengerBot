using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace ChallengerBot
{
    public static class Controller
    {
        public static string GetCurrentVersion(string Location)
        {
            Location += "RADS\\projects\\lol_game_client\\releases\\";
            ASCIIEncoding encoding = new ASCIIEncoding();
            DirectoryInfo dInfo = new DirectoryInfo(Location);
            DirectoryInfo[] subdirs = null;
            try
            {
                subdirs = dInfo.GetDirectories();
            }
            catch { return "0.0.0.0"; }
            string latestVersion = "0.0.1";
            foreach (DirectoryInfo info in subdirs)
            {
                latestVersion = info.Name;
            }

            string AirLocation = Path.Combine(Location, latestVersion, "deploy\\League of Legends.exe");
            FileVersionInfo Version = FileVersionInfo.GetVersionInfo(AirLocation);
            return Version.FileVersion;
        }

        public static string GameClientLocation(string GamePath)
        {
            GamePath += "RADS\\solutions\\lol_game_client_sln\\releases\\";
            ASCIIEncoding encoding = new ASCIIEncoding();
            DirectoryInfo dInfo = new DirectoryInfo(GamePath);
            DirectoryInfo[] subdirs = null;
            try
            {
                subdirs = dInfo.GetDirectories();
            }
            catch { return "0.0.0.0"; }
            string latestVersion = "0.0.1";
            foreach (DirectoryInfo info in subdirs)
            {
                latestVersion = info.Name;
            }

            return Path.Combine(GamePath, latestVersion, "deploy\\");
        }

        public static void Restart()
        {
            var BotClient = Process.Start("ChallengerBot.exe");

            if (Session.IsOpen())
                Session.CloseSession();
       
            Thread.Sleep(1000);
            Environment.Exit(0);
            return;
        }
    } 

    public static class Session
    {
        private static List<Tuple<string, string>> DebugMessages = new List<Tuple<string, string>>();
        private static string TimeString = Core.Time();
        private static string SessionName = null;
        private static string Account = null;
        private static bool Disabled = true;

        public static void OpenSession(string SessionAccount)
        {
            SessionName = "GameID - " + Random();
            Account = SessionAccount;
            return;
        }

        public static void WriteMessage(string message, bool time = true)
        {
            var MessageTime = (time) ? TimeString : "";
            DebugMessages.Add(new Tuple<string, string>(Account, MessageTime + message));
            return;
        }

        public static string Random()
        {
            var chars = "qwertyuiopasdfhjklzxcvbnmABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var result = new string(
                Enumerable.Repeat(chars, 8)
                          .Select(s => s[random.Next(s.Length)])
                          .ToArray());

            return result;
        }

        public static void CloseSession()
        {
            if (DebugMessages.Count < 1 || Disabled)
                return;

              
            using (StreamWriter Session = new StreamWriter("PVPNetConnection\\SessionDebug\\" + SessionName + ".txt", true))
            {
                Session.Write(JsonConvert.SerializeObject(DebugMessages, Formatting.Indented));
                Session.Close();
            }

            SessionName = null;
            return;
        }

        public static bool IsOpen()
        {
            return (DebugMessages.Count > 0);
        }
    }
}
