using System.Collections.Generic;
using Game.Events.UI;
using Game.UI.Game;
using Mirror;

namespace Game.Network.Messages
{
    public struct ClientRequestMapLoad : NetworkMessage { }
    public struct ServerConfirmPlayerEnteredMatch : NetworkMessage { }

    public struct ServerRefreshLeaderboard : NetworkMessage
    {
        public GuidItemPair[] items;
    }

    public struct ServerUpdatePlayerOnLeaderboard : NetworkMessage
    {
        public ChangeLeaderboardItem item;
    }
}