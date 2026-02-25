using Game.Core.Hits;
using Game.Core.Hits.Events;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Hits
{
    public class KnockbackTarget : HitListener
    {
        public UnityEvent<Vector3> onKnockback = new();

        private void Awake()
        {
            onHit.AddListener(OnHit);
        }

        private void OnHit(HitEvent hitEvent)
        {
            if (hitEvent.source is not KnockbackSource knockbackSource) return;
            var direction = (hitEntity.Collider.bounds.center - knockbackSource.hitEntity.Collider.bounds.center).normalized;
            onKnockback.Invoke(direction * knockbackSource.knockbackForce);
        }
    }
}