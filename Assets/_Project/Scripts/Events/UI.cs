using System;
using Game.Core.Events;
using Game.UI.Game;

// TODO: ts is bad
namespace Game.Events.UI
{
    public struct RequestGameplayUI : IEvent { }
    public struct RespawnEffectRequest : IEvent { }

    public struct LeaderboardEventData
    {
        public Guid guid;
        public LeaderboardItem item;
    }

    public struct ClearLeaderboard : IEvent { }

    public struct PopulateLeaderboard : IEvent
    {
        public LeaderboardEventData[] itemDatas;
    }

    public struct AddLeaderboardItem : IEvent
    {
        public LeaderboardEventData itemData;
    }

    public struct RemoveLeaderboardItem : IEvent
    {
        public Guid guid;
    }

    public struct ChangeLeaderboardItem : IEvent
    {
        public LeaderboardEventData itemData;
    }
}