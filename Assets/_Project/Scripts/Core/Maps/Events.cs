using Game.Core.Events;
using Game.Core.Player;

namespace Game.Core.Maps.Events
{
    public struct OnAddPlayerToMap : IEvent
    {
        public PlayerCore player;
    }

    public struct OnUnloadMap : IEvent { }
    public struct OnMapUnloaded : IEvent { }
}