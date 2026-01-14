using Game.Core.Events;
using Game.Core.Projectiles;
using UnityEngine;

namespace Game.Events.HitWatcher
{
    public struct OnDamageDealerCreate : IEvent
    {
        public DamageDealer dealer;
    }

    public struct OnDamageDealerDestroy : IEvent
    {
        public DamageDealer dealer;
    }

    public struct RequestTwoPointsDealerCheck : IEvent
    {
        public DamageDealer dealer;
        public Vector3 point1;
        public Vector3 point2;
    }

    public struct OnHitsRegisteredThisFrame : IEvent { }
}