using Game.Core.Events;
using Game.UI.Game;

// TODO: ts is bad
namespace Game.Events.UI
{
    public struct RequestGameplayUI : IEvent { }
    public struct RespawnEffectRequest : IEvent { }

    public struct LeaderboardEventData
    {
        public string guid;
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
        public string guid;
    }

    public struct ChangeLeaderboardItem : IEvent
    {
        public LeaderboardEventData itemData;
    }
}