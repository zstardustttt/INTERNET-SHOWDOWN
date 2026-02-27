using Game.Core.Events;
using Game.Player.Health;

namespace Game.Player.Events
{
    public struct OnPlayerHealthChanged : IEvent
    {
        public PlayerHealthModule healthModule;
        public float oldHealth;
        public float newHealth;
    }

    public struct OnLocalPlayerAddedToMap : IEvent { }
    public struct OnLocalPlayerRemovedFromMap : IEvent { }
}