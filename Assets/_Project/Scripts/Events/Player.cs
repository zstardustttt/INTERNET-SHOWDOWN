using Game.Core.Events;
using Game.Player;

namespace Game.Events.Player
{
    public struct OnDestroyPlayer : IEvent { }
    public struct OnStatsChanged : IEvent
    {
        public PlayerBase player;
    }
}