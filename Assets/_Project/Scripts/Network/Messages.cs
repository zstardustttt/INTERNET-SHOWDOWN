using System;
using Game.Events.UI;
using Mirror;

namespace Game.Network.Messages
{
    public struct ClientRequestMapLoad : NetworkMessage { }
    public struct ServerOnlinePlayerAddedToMap : NetworkMessage { }
    public struct ServerOnlinePlayerRemovedFromMap : NetworkMessage { }

    public struct ServerClearLeaderboard : NetworkMessage { }

    public struct ServerPopulateLeaderboard : NetworkMessage
    {
        public LeaderboardEventData[] itemDatas;
    }

    public struct ServerAddLeaderboardItem : NetworkMessage
    {
        public LeaderboardEventData itemData;
    }

    public struct ServerRemoveLeaderboardItem : NetworkMessage
    {
        public Guid guid;
    }

    public struct ServerChangeLeaderboardItem : NetworkMessage
    {
        public LeaderboardEventData itemData;
    }
}