using Game.Core.Events;

namespace Game.Core.Player.Stats
{
    public struct OnPlayerStatsChanged : IEvent
    {
        public PlayerCore player;
        public PlayerStats previous;
        public PlayerStats current;
    }
}