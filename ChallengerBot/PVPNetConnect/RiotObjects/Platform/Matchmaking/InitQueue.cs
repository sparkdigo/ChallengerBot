using PVPNetConnect.Queue;

namespace PVPNetConnect.RiotObjects.Platform.Matchmaking
{
    public class InitializeQueue : RiotGamesObject
    {
        public override string TypeName
        {
            get { return this.type; }
        }

        private string type = "com.riotgames.platform.matchmaking.InitializeQueue";

        public InitializeQueue()
        {
        }

        public InitializeQueue(TypeList data)
        {
            this.QueueInfo = data;
        }

        [InternalName("QueueInfo")]
        public TypeList QueueInfo { get; set; }
    }
}