using System;
using Game.Core.Damage;
using Game.Core.Events;
using UnityEngine;

namespace Game.Events.HitWatcher
{
    public struct OnDamageDealerCreate : IEvent
    {
        public DamageDealer dealer;
    }

    public struct OnDamageDealerDestroy : IEvent
    {
        public Guid guid;
    }

    public struct OnDamageReceiverRegister : IEvent
    {
        public DamageReceiver receiver;
    }

    public struct OnDamageReceiverUnregister : IEvent
    {
        public Guid guid;
    }

    public struct RequestTwoPointsDealerCheck : IEvent
    {
        public DamageDealer dealer;
        public Vector3 point1;
        public Vector3 point2;
    }

    public struct OnRegisterHit : IEvent
    {
        public DamageDealer dealer;
        public DamageReceiver receiver;
    }
}