#region System
using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Net.Http;
using Timer = System.Timers.Timer;
#endregion

#region PVPNetConnect
using PVPNetConnect;
using PVPNetConnect.Queue;
using PVPNetConnect.RiotObjects.Platform.Catalog.Champion;
using PVPNetConnect.RiotObjects.Platform.Clientfacade.Domain;
using PVPNetConnect.RiotObjects.Platform.Game;
using PVPNetConnect.RiotObjects.Platform.Matchmaking;
using PVPNetConnect.RiotObjects.Platform.Statistics;
using PVPNetConnect.RiotObjects.Platform.Gameinvite.Contract;
using PVPNetConnect.RiotObjects.Team.Dto;
using PVPNetConnect.RiotObjects.Platform.Systemstate;
#endregion


/*
    _________ .__           .__  .__                                    __________        __   
    \_   ___ \|  |__ _____  |  | |  |   ____   ____    ____   __________\______   \ _____/  |_ 
    /    \  \/|  |  \\__  \ |  | |  | _/ __ \ /    \  / ___\_/ __ \_  __ \    |  _//  _ \   __\
    \     \___|   Y  \/ __ \|  |_|  |_\  ___/|   |  \/ /_/  >  ___/|  | \/    |   (  <_> )  |  
     \______  /___|  (____  /____/____/\___  >___|  /\___  / \___  >__|  |______  /\____/|__|  
            \/     \/     \/               \/     \//_____/      \/             \/             
 */

namespace ChallengerBot
{
    internal class Engine
    {
        public PlayerDTO Hero = new PlayerDTO();
        public LoginDataPacket Packets = new LoginDataPacket();
        public PVPNetConnection Connections = new PVPNetConnection();
        public ChampionDTO[] HeroesArray;
        public Process LeagueProcess;

        public string AccountName;
        public string SummonerName;
        public string SummonerQueue;
        public double SummonerLevel;
        public double SummonerID;
        public int    SummonerGameID;

        public bool PlayerAcceptedInvite = false;
        public bool LetPlayerCreateLobby = true;


        public bool FirstSelection = false;
        public bool FirstQueue = true;

        public Engine(string username, string SummonerPassword, string Queues)
        {
            Region CurRegion = (Region)Enum.Parse(typeof(Region), Core.Region);
            SummonerQueue = Queues;
            AccountName = username;

            #region Callbacks

            // OnError
            Connections.OnError += (object sender, Error Error) =>
            {
                if (Error.Type == ErrorType.MaxLevelReached)
                {
                    Core.Blacklist(AccountName);
                    Core.Connect();
                    Connections.Disconnect();
                    return;
                }

                Core.Status("Error received: " + Error.Message, AccountName);
                return;
            };

            Connections.OnLogin += new PVPNetConnection.OnLoginHandler(OnLogin);
            Connections.OnMessageReceived += new PVPNetConnection.OnMessageReceivedHandler(OnMessageReceived);
            #endregion

           // Connections.Connect(AccountName, SummonerPassword, CurRegion, Core.ClientVersion);
            Console.WriteLine(Core.ClientVersion);
            return;
        }

        private void OnLogin(object sender, string username, string ipAddress)
        {
            new Thread((ThreadStart)(async () =>
            {
                try
                {
                    Packets = await Connections.GetLoginDataPacketForUser();
                }
                catch(NotSupportedException)
                {
                    // Restarting BotClient;
                    Controller.Restart();
                }
                    
                await Connections.Subscribe("bc", Packets.AllSummonerData.Summoner.AcctId);
                await Connections.Subscribe("cn", Packets.AllSummonerData.Summoner.AcctId);
                await Connections.Subscribe("gn", Packets.AllSummonerData.Summoner.AcctId);

                if (Packets.AllSummonerData == null)
                    NewPlayerAccout();

                if (Packets.AllSummonerData.Summoner.ProfileIconId == -1)
                    SetSummonerIcon();

                SummonerLevel   = Packets.AllSummonerData.SummonerLevel.Level;
                SummonerName    = Packets.AllSummonerData.Summoner.Name;
                SummonerID      = Packets.AllSummonerData.Summoner.SumId;

                // Session
                Session.OpenSession(AccountName);
                Session.WriteMessage("Player (lv. " + SummonerLevel + ")" + SummonerName + " successfully logged in.");
                Session.WriteMessage("IP Balance before game: " + Packets.IpBalance);

                if (SummonerLevel > Core.MaxLevel || SummonerLevel == Core.MaxLevel)
                {
                    Connections.Error("Summoner has reached maximum level!", ErrorType.MaxLevelReached);
                    Session.WriteMessage("Player " + SummonerName + " successfully reached max set level.");
                    return;
                }
                    
                HeroesArray = await Connections.GetAvailableChampions();
                Hero        = await Connections.CreatePlayer();
                OnMessageReceived(sender, new ClientBeforeStart());
                Thread.Sleep(2000);

            })).Start();
        }

        public async void OnMessageReceived(object sender, object message)
        {
            Debug.WriteLine("Calling message: " + message.GetType());
            if (message is ClientBeforeStart)
            {
                if (Packets.ReconnectInfo != null && Packets.ReconnectInfo.Game != null)
                {
                    OnMessageReceived(sender, (object)Packets.ReconnectInfo.PlayerCredentials);
                    return;
                }

                Core.Status("Connection succeed.", AccountName);
                Core.Accounts.Add(AccountName);

                var Players = Core.Accounts.Count();
                var LastPlayer = Core.Accounts.LastOrDefault();
              
                if (Players == Core.MaxBots && LastPlayer.Equals(AccountName))
                {
                    Core.Status("Bots connected.", AccountName);
                    Timer CreatePremade = new Timer { Interval = 3000, AutoReset = false };
                    CreatePremade.Elapsed += (ek, eo) =>
                    {
                        CreatePremade.Stop();
                        Session.WriteMessage("Player " + SummonerName + " will create lobby.");
                        OnMessageReceived(sender, (object)new CreateLobby());
                    };
                    CreatePremade.Start();
                    return;
                }
                else
                {
                    Session.WriteMessage("Player " + SummonerName + " is waiting for invite.");
                    Core.Register(SummonerID);
                }
            }
            else if (message is CreateLobby)
            {
                Types TypeGame = (Types)Enum.Parse(typeof(Types), SummonerQueue);
                GameQueueConfig Game = new GameQueueConfig();
                Game.Id = TypeGame.GetHashCode();
                SummonerGameID = Convert.ToInt32(Game.Id);

                if (SummonerGameID == 25 || SummonerGameID == 32 || SummonerGameID == 33 || SummonerGameID == 52)
                {
                    Core.Lobby = await Connections.createArrangedBotTeamLobby(Game.Id, "MEDIUM");
                    Core.BotLevel = "MEDIUM";
                }
                else
                    Core.Lobby = await Connections.createArrangedTeamLobby(Game.Id);


                PlayerAcceptedInvite = true;
                Core.Status("Lobby successfully created.", AccountName);
                Session.WriteMessage("Player " + SummonerName + " have created lobby. Inviting other players..");

                // Invite other players.
                foreach (var bot in Core.SummonerIDs)
                {
                    await Connections.Invite(bot);
                }

                return;
            }
            else if (message is InvitationRequest)
            {
                var Invitation = message as InvitationRequest;

                if (Invitation.InvitationId == Core.Lobby.InvitationID && PlayerAcceptedInvite == false)
                {
                    Core.Lobby = await Connections.AcceptLobby(Invitation.InvitationId);
                    PlayerAcceptedInvite = true;
                    Core.Status("Invitation accepted.", AccountName);
                    Session.WriteMessage("Player " + SummonerName + " accepted invite.");
                    return;
                }
            }
            else if (message is LobbyStatus)
            {
                if (Core.Lobby == null)
                    return;

                #region Debugging errors.
                List<string> Errors = new List<string>();
                if (SummonerName != Core.Lobby.Owner.SummonerName)
                    Errors.Add("Trying to access LobbyStatus not as owner. Exiting...");
                if (Core.LobbyStatusWaiting)
                    Errors.Add("Currently waiting for all players.");

                if (Errors.Count > 0)
                {
                    Debug.WriteLine("-----------------------------");
                    Debug.WriteLine("LobbyStatus was terminated due following errors:");
                    foreach (var msg in Errors)
                    {
                        Debug.WriteLine("        " + msg);
                    }
                    Debug.WriteLine("-----------------------------");
                    return;
                } 
                #endregion

                if (Core.Lobby.Members.Count < Core.MaxBots && !Core.LobbyStatusWaiting)
                {
                    Core.LobbyStatusWaiting = true;
                    while (Core.Lobby.Members.Count < Core.MaxBots)
                        Thread.Sleep(100);
                }
  
                var LobbyInfo = Core.Lobby;
                Core.Status("All members has joined into the lobby.", AccountName);
                
                
                #region Queue
                Core.LobbyGame.QueueIds = new Int32[1] { (int)SummonerGameID };
                Core.LobbyGame.InvitationId = LobbyInfo.InvitationID;
                var InviteList = new List<int>();
                foreach (Member stats in LobbyInfo.Members)
                {
                    int GameInvitePlayerList = Convert.ToInt32(stats.SummonerId);
                    InviteList.Add(GameInvitePlayerList);
                }
                Core.LobbyGame.Team = InviteList;
                Core.LobbyGame.BotDifficulty = Core.BotLevel;
                #endregion
                
                OnMessageReceived(sender, await Connections.AttachTeamToQueue(Core.LobbyGame));
                Session.WriteMessage("Player " + SummonerName + " has started matchmaking...");
                Core.Status("Team has been attached to queue", AccountName);
                return;
            }
            else if (message is GameDTO)
            {
                
                GameDTO game = message as GameDTO;
                switch (game.GameState)
                {
                    case "CHAMP_SELECT":
                        if (FirstSelection)
                            break;

                        FirstSelection = true;
                        Core.Status("Champion select in.", AccountName);
                        await Connections.SetClientReceivedGameMessage(game.Id, "CHAMP_SELECT_CLIENT");

                        if (SummonerGameID != 65)
                        {
                            var hArray = HeroesArray.Shuffle();
                            await Connections.SelectChampion(hArray.First(HR => HR.FreeToPlay == true || HR.Owned == true).ChampionId);
                            await Connections.ChampionSelectCompleted();
                            break;
                        }

                        break;
                    case "POST_CHAMP_SELECT":
                        FirstQueue = true;
                        //Core.Status("Post champion select.", AccountName);
                        break;
                    case "PRE_CHAMP_SELECT":
                        break;
                    case "GAME_START_CLIENT":
                        Core.Status("Lauching League of Legends.", AccountName);
                        break;
                    case "GameClientConnectedToServer":
                        break;
                    case "IN_QUEUE":
                        Core.Status("Waiting for game.", AccountName);
                        break;
                    case "TERMINATED":
                        FirstQueue = true;
                        PlayerAcceptedInvite = false;
                        Core.Status("Re-entering queue.", AccountName);
                        break;
                    case "JOINING_CHAMP_SELECT":
                        if (FirstQueue)
                        {
                            Core.Status("Game accepted!", AccountName);
                            FirstQueue = false;
                            FirstSelection = false;
                            await Connections.AcceptPoppedGame(true);
                            break;
                        }
                        break;
                    case "LEAVER_BUSTED":
                        Core.Status("Leave Busted!", AccountName);
                        break;
                }
            }
            else if (message is PlayerCredentialsDto)
            {

                string GameLocation = Controller.GameClientLocation(Core.GamePath);
                PlayerCredentialsDto credentials = message as PlayerCredentialsDto;
                ProcessStartInfo startInfo = new ProcessStartInfo();

                
                startInfo.CreateNoWindow = false;
                startInfo.WorkingDirectory = GameLocation;
                startInfo.FileName = "League of Legends.exe";
                startInfo.Arguments = "\"8394\" \"LoLLauncher.exe\" \"\" \"" + credentials.ServerIp + " " +
                credentials.ServerPort + " " + credentials.EncryptionKey + " " + credentials.SummonerId + "\"";
                Core.Status("Launching League of Legends", AccountName);
                new Thread((ThreadStart)(() =>
                {
                    LeagueProcess = Process.Start(startInfo);
                    LeagueProcess.Exited += LeagueProcess_Exited;
                    while (LeagueProcess.MainWindowHandle == IntPtr.Zero) ;
                    LeagueProcess.PriorityClass = ProcessPriorityClass.Idle;
                    LeagueProcess.EnableRaisingEvents = true;
                    
                })).Start();
            }
            if (message is EndOfGameStats)
            {
                Core.Accounts.Clear();
                Core.SummonerIDs.Clear();
                Core.LobbyStatusWaiting = false;

                // Process kill
                LeagueProcess.Exited -= LeagueProcess_Exited;
                LeagueProcess.Kill();
                Thread.Sleep(500);
                
                if (LeagueProcess.Responding)
                    Process.Start("taskkill /F /IM \"League of Legends.exe\"");

                // Level check.
                Packets = await Connections.GetLoginDataPacketForUser();
                var ASLevel = SummonerLevel;
                var SLevel = Packets.AllSummonerData.SummonerLevel.Level;
                
                if (SLevel != ASLevel)
                    OnPlayerLevel();

                Session.CloseSession();
                Thread.Sleep(2000);
                OnMessageReceived(sender, new ClientBeforeStart());
                return;
            }
            else if (message is SearchingForMatchNotification)
            {
                var result = message as SearchingForMatchNotification;

                if (result.PlayerJoinFailures != null)
                {
                    List<Tuple<string, int>> Summoners = new List<Tuple<string, int>>();
                    string AccessToken = null;
                    bool Penalty = false;
                    

                    foreach (var item in result.PlayerJoinFailures)
                    {
                        var x = new QueueDodger(item as TypedObject);
                        if (x.ReasonFailed == "LEAVER_BUSTED")
                        {
                            AccessToken = x.AccessToken;
                            Summoners.Add(new Tuple<string, int>(x.Summoner.Name, x.LeaverPenaltyMillisRemaining));
                            Penalty = true;

                        }
                        else
                        {
                            Core.Status("Reason: " + x.ReasonFailed, AccountName);
                            return;
                        }
                    }

                    if (Penalty)
                    {
                        Debug.WriteLine("Penalty timer.");
                        var TimeWait = Summoners.OrderByDescending(s => s.Item2).FirstOrDefault().Item2;
                        var Time = TimeSpan.FromMilliseconds(TimeWait);
                        var Players = string.Join(",", Summoners.Select(s => s.Item1).ToArray());
                        Debug.WriteLine("Time wait" + TimeWait + "ms." + "Counted summoners: " + Summoners.Count + "; Summoners: " + Players);
                        Core.Status("Waiting " + Time.Minutes + " to be able to join queue", AccountName);
                        Thread.Sleep(TimeWait + 5000);
                        Session.WriteMessage("Player " + SummonerName + " successfully joined lower queue!");

                        if (SummonerName == Core.Lobby.Owner.SummonerName)
                        {
                            OnMessageReceived(sender, await Connections.AttachToQueue(Core.LobbyGame, AccessToken));
                        }
                    }
                }
                else
                {
                    //Core.Status("Wait time: " + result.JoinedQueues.First(namae => namae.QueueId == SummonerID).WaitTime, AccountName);
                }
            }
        }

        private async void OnPlayerLevel()
        {
            /*
             * TODO: Take some actions when player reaches maximum level.
             * if (SummonerLevel >= Core.MaxLevel)
            {
                Connections.Disconnect();
                if (!Connections.IsConnected())
                {
                    Core.Blacklist(AccountName);
                    Core.Connect();
                }
            }*/

            if (Packets.RpBalance > 260)
            {
                string url = await Connections.GetStoreUrl();
                HttpClient httpClient = new HttpClient();
                Debug.WriteLine("Store url: " + url);
                await httpClient.GetStringAsync(url);
                string storeURL = "https://store.eun1.lol.riotgames.com/store/tabs/view/boosts/1";
                await httpClient.GetStringAsync(storeURL);
                string purchaseURL = "https://store.eun1.lol.riotgames.com/store/purchase/item";
                List<KeyValuePair<string, string>> storeItemList = new List<KeyValuePair<string, string>>();
                storeItemList.Add(new KeyValuePair<string, string>("item_id", "boosts_2"));
                storeItemList.Add(new KeyValuePair<string, string>("currency_type", "rp"));
                storeItemList.Add(new KeyValuePair<string, string>("quantity", "1"));
                storeItemList.Add(new KeyValuePair<string, string>("rp", "260"));
                storeItemList.Add(new KeyValuePair<string, string>("ip", "null"));
                storeItemList.Add(new KeyValuePair<string, string>("duration_type", "PURCHASED"));
                storeItemList.Add(new KeyValuePair<string, string>("duration", "3"));
                HttpContent httpContent = new FormUrlEncodedContent(storeItemList);
                await httpClient.PostAsync(purchaseURL, httpContent);
                Core.Status("Bought XP boost!", AccountName);
                Session.WriteMessage("Player " + SummonerName + " has bought XP boost!");
                httpClient.Dispose();
            }
        }

        private async void LeagueProcess_Exited(object sender, EventArgs e)
        {
            Core.Status("Restart League of Legends.", AccountName);
            Packets = await Connections.GetLoginDataPacketForUser();
            if (Packets.ReconnectInfo != null && Packets.ReconnectInfo.Game != null)
            {
                Session.WriteMessage("Game is going to be restored!");
                OnMessageReceived(sender, (object)Packets.ReconnectInfo.PlayerCredentials);
            }                
        }

        private async void NewPlayerAccout()
        {
            String summonerName = SummonerName;

            if (summonerName.Length > 16)
                summonerName = summonerName.Substring(0, 12) + new Random().Next(1000, 9999).ToString();

            await Connections.CreateDefaultSummoner(summonerName);
            Core.Status("Created summoner: " + summonerName, AccountName);
            Session.WriteMessage("Player " + summonerName + " has been created!");
        }

        private async void SetSummonerIcon()
        {
            await Connections.UpdateProfileIconId(12);
            return;
        }
    }

    public static class EnumerableExtensions
    {
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            return source.Shuffle(new Random());
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random rng)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (rng == null) throw new ArgumentNullException("rng");

            return source.ShuffleIterator(rng);
        }

        private static IEnumerable<T> ShuffleIterator<T>(
            this IEnumerable<T> source, Random rng)
        {
            List<T> buffer = source.ToList();
            for (int i = 0; i < buffer.Count; i++)
            {
                int j = rng.Next(i, buffer.Count);
                yield return buffer[j];

                buffer[j] = buffer[i];
            }
        }
    }
}
