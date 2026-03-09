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

            var sharingFamily = source.teamReference.Unwrap().CompareTeam(target.teamReference.Unwrap());
            if (!sharingFamily || source.canDamageTeam)
            {
                var evaluation = source.EvaluateDamage(target);
                if (!evaluation.valid) return;

                var damage = new Damage(evaluation.type, evaluation.amount, source.authorReference.Unwrap(), source.Guid, source.teamReference.Unwrap());
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