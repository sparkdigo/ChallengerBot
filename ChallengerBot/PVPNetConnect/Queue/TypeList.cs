namespace PVPNetConnect.Queue
{
    // Gamemodes and their IDs: http://prntscr.com/6r8wq6
    public enum Types
    {
        // Summoners Rift. Normal. 5x5
        SRN = 2,
        // Twisted Three Line. Normal. 3x3
        TTN = 8,
        // Summoners Rift. CO vs. AI. 5x5 (Easy).
        SRB = 32,
        // Summoners Rift. CO vs. AI. 5x5 (Intermediate).
        SRI = 33,
        // Howling Abyss. Normal. 5x5
        HAN = 65,
        // Crystal Scar. Bot game. 5x5
        ODB = 25
    }

    public class TypeList
    {
        public Types GameType;
        public bool GatherPremade = false;
    }
}