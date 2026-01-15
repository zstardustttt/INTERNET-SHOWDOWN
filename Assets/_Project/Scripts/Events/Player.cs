using Game.Core.Events;
using Game.Player;

namespace Game.Events.Player
{
    public struct OnDestroyPlayer : IEvent
    {
        public string guid;
    }

    public struct OnStatsChanged : IEvent
    {
        public PlayerBase player;
    }

    public struct OnServerOnlinePlayerInitialized : IEvent
    {
        public PlayerBase player;
    }
}