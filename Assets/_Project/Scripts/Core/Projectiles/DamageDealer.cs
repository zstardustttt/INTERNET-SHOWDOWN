using Game.Core.Events;
using Game.Events.HitWatcher;
using Game.Player;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Core.Projectiles
{
    [RequireComponent(typeof(Collider))]
    public abstract class DamageDealer : NetworkBehaviour
    {
        public abstract bool Direct { get; }

        [HideInInspector] public Vector3 previousObservedPosition;
        [HideInInspector] public Vector3 observedDelta;
        [HideInInspector] public int hitScanCount;

        [HideInInspector] public PlayerBase owner;
        [HideInInspector] public Collider coll;

        [Tooltip("Allows only one hit scan per dealer's lifetime")] public bool singleHitScan;
        public UnityEvent<PlayerBase, float> OnHit = new();
        public abstract float EvaluateDamage(PlayerBase player);

        protected override void OnValidate()
        {
            base.OnValidate();
            coll = GetComponent<Collider>();
        }

        private void Awake()
        {
            EventBus<OnDamageDealerCreate>.Invoke(new() { dealer = this });
        }

        private void OnDestroy()
        {
            EventBus<OnDamageDealerDestroy>.Invoke(new() { dealer = this });
        }
    }
}