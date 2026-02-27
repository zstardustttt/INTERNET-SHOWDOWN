using Game.Core.Events;
using Game.Player;

namespace Game.Events.MapLoader
{
    public struct OnAddPlayerToMap : IEvent
    {
        public PlayerBase player;
    }

    public struct OnUnloadMap : IEvent { }
}