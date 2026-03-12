using Game.Core.Damages.Events;
using Game.Core.Hits;
using UnityEngine.Events;

namespace Game.Core.Damages
{
    public struct DamageEvaluation
    {
        public bool valid;
        public DamageType type;
        public float amount;

        public DamageEvaluation(DamageType type, float amount)
        {
            valid = true;
            this.type = type;
            this.amount = amount;
        }
    }

    public class DamageSource : HitListener
    {
        public DamageIdentification Identification { get; private set; }

        public DamageIdentificationSetup damageIdentificationSetup;
        public AuthorReference authorReference;
        public TeamReference teamReference;
        public bool canDamageTeam;
        public UnityEvent<DamageEvent> onWishDamage;
        public UnityEvent<DamageEvent> onDamage;

        public override void OnStartServer()
        {
            Identification = DamageIdentification.From(damageIdentificationSetup);
        }

        public virtual DamageEvaluation EvaluateDamage(DamageTarget target)
        {
            return default;
        }
    }
}