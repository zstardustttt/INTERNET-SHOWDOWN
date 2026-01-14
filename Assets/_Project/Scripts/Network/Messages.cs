using Game.Events.UI;
using Mirror;

namespace Game.Network.Messages
{
    public struct ClientRequestMapLoad : NetworkMessage { }
    public struct ServerConfirmPlayerEnteredMatch : NetworkMessage { }

    public struct ServerUpdateLeaderboard : NetworkMessage
    {
        public AddToLeaderboard[] leaderboardItems;
    }
}