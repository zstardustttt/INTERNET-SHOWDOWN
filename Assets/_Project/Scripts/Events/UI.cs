using Game.Core.Events;

namespace Game.Events.UI
{
    public struct RequestGameplayUI : IEvent { }
    public struct OnHealthUpdate : IEvent
    {
        public float health;
        public float maxHealth;
    }
}