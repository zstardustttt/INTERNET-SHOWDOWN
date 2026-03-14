using Game.Core.Damages.Events;
using Game.Core.Events;
using Game.Core.Hits.Events;
using Game.Core.Player.Health.Events;
using UnityEngine;

namespace Game.Core.Damages
{
    public class DamageWatcher : MonoBehaviour
    {
        private void Awake()
        {
            EventBus<HitEvent>.Listen(OnHit);
            EventBus<OnPlayerDamage>.Listen(OnPlayerDamage);
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

                var author = source.authorReference.Unwrap();
                var damage = new Damage(evaluation.type, evaluation.amount, author, source.teamReference.Unwrap(), source.Identification);
                var damageEvent = new DamageEvent(source, target, damage);

                source.onWishDamage.Invoke(damageEvent);
                target.onWishDamage.Invoke(damageEvent);
                if (!target.ApplyDamage(damage)) return;

                source.onDamage.Invoke(damageEvent);
                target.onDamage.Invoke(damageEvent);
                EventBus<DamageEvent>.Invoke(damageEvent);

                if (author) author.ReportDealtDamage(target, damage);
            }
        }

        private void OnPlayerDamage(OnPlayerDamage data)
        {
            if (data.damage.author && !data.player.teamReference.Unwrap().CompareTeam(data.damage.team))
            {
                data.damage.author.ReportDealtDamageOnPlayer
                (
                    data.player,
                    data.damage,
                    data.finalAmount
                );
            }
        }
    }
}