using Game.Core.Events;
using Game.Events.HitWatcher;
using Game.Player;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Core.Projectiles
{
    [RequireComponent(typeof(Collider))]
    public abstract class DamageDealer : MonoBehaviour
    {
        public Vector3 previousObservedPosition;
        public Vector3 observedDelta;

        public PlayerBase owner;
        public Collider coll;

        public UnityEvent<PlayerBase, float> OnHit = new();
        public abstract float EvaluateDamage(PlayerBase player);

        private void OnValidate()
        {
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