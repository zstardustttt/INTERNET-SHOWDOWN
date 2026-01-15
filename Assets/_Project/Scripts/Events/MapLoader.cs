using Game.Core.Events;
using Game.Player;

namespace Game.Events.MapLoader
{
    public struct OnAddPlayerOnMap : IEvent
    {
        public PlayerBase player;
    }

    public struct OnUnloadMap : IEvent { }
}