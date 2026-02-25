using Game.Core.Events;
using Game.UI.Game;

namespace Game.Events.UI
{
    public struct RequestGameplayUI : IEvent { }
    public struct HitIndicatorRequest : IEvent { }
    public struct PureKillIndicatorRequest : IEvent { }
    public struct UnpureKillIndicatorRequest : IEvent { }
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