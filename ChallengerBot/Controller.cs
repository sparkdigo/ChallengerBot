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
       
            Thread.Sleep(1000);
            Environment.Exit(0);
            return;
        }
    }
}
