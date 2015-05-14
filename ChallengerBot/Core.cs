using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using PVPNetConnect.RiotObjects.Platform.Gameinvite.Contract;
using PVPNetConnect.RiotObjects.Platform.Matchmaking;
using Timer = System.Timers.Timer;

namespace ChallengerBot
{
    public static class Core
    {
        // Client
        public static string ClientVersion;
        public static int LastPlayerNameLenght = 0;
        public static LobbyStatus Lobby;
        public static MatchMakerParams LobbyGame = new MatchMakerParams();

        // CBot
        public static List<Tuple<string, string, string>> AccountArray = new List<Tuple<string, string, string>>();
        public static List<string> Blacklisted = new List<string>();
        public static List<double> SummonerIDs = new List<double>();
        public static List<string> Accounts = new List<string>();

        // Premade settings
        public static int PremadePlayers = 0;
        public static bool LobbyStatusWaiting = false;
        public static bool LobbyInQueue = false;
        public static int Delay = 30;
        public static int CounterBots = 0;

        // CBots
        public static int Waiting = 0;
        public static int ConnectedBots = 0;
        public static int LoadedBots = 0;
        public static string BotLevel = null;

        // Botting Settings
        public static string GamePath { get { return ChallengerConfig.GamePath; } }
        public static string PlayHero { get { return ChallengerConfig.PlayHero; } }
        public static string Region { get { return ChallengerConfig.Region; } }
        
        public static int MaxBots { get { return ChallengerConfig.MaxBots; } }
        public static int MaxLevel { get { return ChallengerConfig.MaxLevel; } }

        public static bool XPBoost { get { return ChallengerConfig.XPBoost; } }
        public static bool CreatePremade { get { return ChallengerConfig.CreatePremade; } }
        public static bool GameConfigReplace { get { return ChallengerConfig.GameConfigReplace; } }

        // Messages
        public static bool PlayersInvitedShown = false;

        static void Main(string[] args)
        {
            try
            {
                ChallengerConfig.Init();
            }
            catch (JsonReaderException)
            {
                Console.WriteLine("Error loading ChallengerBot.ini!\r\nMake sure that GamePath contains \\\\ instead of \\\r\nApplication will now exit.");
                Timer Exit = new Timer { Interval = 5000, AutoReset = false };
                Exit.Elapsed += (argo, argt) =>
                {
                    Environment.Exit(0);
                };

                Exit.Start();
                NoExit();
                return;
            }

            if (!File.Exists("accounts.txt"))
            {
                Console.WriteLine(Time() + "File accounts.txt does not exist!");
                NoExit();
                return;
            }
                
            GetBots();
            Console.Title = "ChallengerBot";
            Console.ForegroundColor = ConsoleColor.White;
            Console.SetWindowSize(Console.WindowWidth + 5, Console.WindowHeight);
            Status("Loaded successfuly.", "ChallengerBot");
            Console.WriteLine("\n\r\n\r");
            Connect();
            NoExit();
        }

        private static void NoExit()
        {
            while(true)
                Thread.Sleep(100);
        }
 
        public static void Connect()
        {
            foreach (var PlayerBot in AccountArray)
            {
                if (!Blacklisted.Contains(PlayerBot.Item1))
                {
                    Engine Connection = new Engine(PlayerBot.Item1, PlayerBot.Item2, PlayerBot.Item3);
                    Waiting++;

                    if (Waiting == MaxBots)
                        break;
                }
            }
        }

        public static void ConnectPlayer(string username, string password)
        {
            if (!AccountArray.Any(bot => bot.Item1.Equals(username)))
            {
                Console.WriteLine("Given player not exists in array.");
                return;
            }


            var firstOrDefault = AccountArray.FirstOrDefault(player => player.Item1.Equals(username));
            if (firstOrDefault == null) return;
            var getQueueFromArray = firstOrDefault.Item3;
            new Engine(username, password, getQueueFromArray);
            return;
        }

        private static void GetBots()
        {
            StreamReader file = new StreamReader("accounts.txt");
            string line;

            while ((line = file.ReadLine()) != null)
            {
                string[] account = line.Split('|');
                // First - username; Second - password; Third - QType; Forth - Region
                AccountArray.Add(new Tuple<string, string, string>(account[0], account[1], account[2]));
                LoadedBots++;
            }

            file.Close();
            return;
        }

        public static void Blacklist(string AccountName)
        {
            Blacklisted.Add(AccountName);
            return;
        }

        public static void Register(double sid)
        {
            SummonerIDs.Add(sid);
            return;
        }
        
        public static string Time()
        {
            DateTime Date = DateTime.Now;
            var output = "[" + Date.ToString("HH:mm:ss") + "] ";
            return output;
            
        }
        
        public static void Status(string text, string player)
        {
            var Spacing = GetSpacing(player);
            Console.WriteLine(Time() + Spacing + text);
            Thread.Sleep(250);
            return;
        }


        public static string GetSpacing(string player)
        {
            string result = "[" + player + "] ";

            int difference = player.Length;
  
            var max = AccountArray.OrderByDescending(s => s.Item1.Length).FirstOrDefault();
            if (max != null) difference = max.Item1.Length - player.Length;

            for (int o = 1; o <= difference; o++)
                result += " ";

            return result;
        }

        public static void CenteredText(String text)
        {
            Console.Write(new string(' ', (Console.WindowWidth - text.Length) / 2));
            Console.WriteLine(text);
        }

        public static IEnumerable<T> Randomize<T>(this IEnumerable<T> source)
        {
            Random rnd = new Random();
            return source.OrderBy<T, int>((item) => rnd.Next());
        }
    }
}
