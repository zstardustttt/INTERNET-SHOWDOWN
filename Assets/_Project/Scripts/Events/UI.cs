using Game.Core.Events;

namespace Game.Events.UI
{
    public struct RequestGameplayUI : IEvent { }
    public struct OnHealthUpdate : IEvent
    {
        public float health;
        public float maxHealth;
    }

    public struct HitIndicatorRequest : IEvent { }
    public struct DamageIndicatorRequest : IEvent { }

    public struct ClearLeaderboard : IEvent { }
    public struct AddToLeaderboard : IEvent
    {
        public string name;
        public int directHits;
        public int indirectHits;
    }
}