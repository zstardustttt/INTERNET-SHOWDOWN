using Game.Core.Events;
using Game.Player;

namespace Game.Maps.Events
{
    public struct OnAddPlayerToMap : IEvent
    {
        public PlayerBase player;
    }

    public struct OnUnloadMap : IEvent { }
    public struct OnMapUnloaded : IEvent { }
}