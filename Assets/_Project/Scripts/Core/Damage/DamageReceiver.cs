using System;
using System.Collections.Generic;
using Game.Core.Events;
using Game.Events.HitWatcher;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Core.Damage
{
    [RequireComponent(typeof(Collider))]
    public class DamageReceiver : NetworkBehaviour
    {
        public Guid Guid { get; private set; }
        public bool active = true;
        public UnityEvent<DamageDealer, float> onDamage = new();
        public Collider coll;
        public List<DamageDealer> ignoreDealers = new();

        [HideInInspector] public Vector3 previousObservedPosition;
        [HideInInspector] public Vector3 observedDelta;

        protected override void OnValidate()
        {
            base.OnValidate();
            TryGetComponent(out coll);
        }

        public void Register(Guid guid)
        {
            Guid = guid;
            EventBus<OnDamageReceiverRegister>.Invoke(new() { receiver = this });
        }

        public void Unregister()
        {
            EventBus<OnDamageReceiverUnregister>.Invoke(new() { guid = Guid });
        }

        private void OnDestroy()
        {
            Unregister();
        }
    }
}