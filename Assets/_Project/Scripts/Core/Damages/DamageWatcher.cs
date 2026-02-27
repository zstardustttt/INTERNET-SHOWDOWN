using System;
using Game.Core.Damages.Events;
using Game.Core.Events;
using Game.Core.Hits.Events;
using UnityEngine;

namespace Game.Core.Damages
{
    public class DamageWatcher : MonoBehaviour
    {
        private void Awake()
        {
            EventBus<HitEvent>.Listen(OnHit);
        }

        private void OnHit(HitEvent hitEvent)
        {
            if (hitEvent.source is not DamageSource source) return;
            if (hitEvent.target is not DamageTarget target) return;

            var sharingFamily = source.family != Guid.Empty && target.family != Guid.Empty && source.family == target.family;
            if (!sharingFamily || source.canDamageFamily)
            {
                var evaluation = source.EvaluateDamage(target);
                if (!evaluation.HasValue) return;

                var damage = evaluation.Value;
                var damageEvent = new DamageEvent(source, target, damage);

                source.onWishDamage.Invoke(damageEvent);
                target.onWishDamage.Invoke(damageEvent);
                if (!target.ApplyDamage(damage)) return;

                source.onDamage.Invoke(damageEvent);
                target.onDamage.Invoke(damageEvent);
                EventBus<DamageEvent>.Invoke(damageEvent);
            }
        }
    }
}