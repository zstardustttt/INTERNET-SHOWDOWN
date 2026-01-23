using System;
using Game.Core.Events;
using Game.Events.HitWatcher;
using Game.Player;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Core.Damage
{
    public enum DamageType
    {
        None,
        Direct,
        Indirect,
        Continuous
    }

    [RequireComponent(typeof(Collider))]
    public abstract class DamageDealer : NetworkBehaviour
    {
        public Guid DealerGuid { get; private set; }

        [HideInInspector] public Vector3 previousObservedPosition;
        [HideInInspector] public Vector3 observedDelta;
        [HideInInspector] public int hitScanCount;

        [HideInInspector] public PlayerBase owner;
        [HideInInspector] public Collider coll;

        public bool active = true;
        public DamageType damageType;
        [Tooltip("Allows only one hit scan per dealer's lifetime")] public bool singleHitScan;
        public bool canDamageOwner;
        public float knockbackForce;
        public UnityEvent<DamageReceiver, float> onHit = new();
        public abstract float EvaluateDamage(DamageReceiver receiver);

        protected override void OnValidate()
        {
            base.OnValidate();
            coll = GetComponent<Collider>();
            coll.isTrigger = true;
        }

        private void Awake()
        {
            DealerGuid = Guid.NewGuid();
            previousObservedPosition = transform.position;
            EventBus<OnDamageDealerCreate>.Invoke(new() { dealer = this });
        }

        private void OnDestroy()
        {
            EventBus<OnDamageDealerDestroy>.Invoke(new() { guid = DealerGuid });
        }
    }
}