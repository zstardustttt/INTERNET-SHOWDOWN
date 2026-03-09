using System;
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
        public Guid Guid { get; private set; }

        public AuthorReference authorReference;
        public TeamReference teamReference;
        public bool canDamageTeam;
        public UnityEvent<DamageEvent> onWishDamage;
        public UnityEvent<DamageEvent> onDamage;

        private void Awake()
        {
            Guid = Guid.NewGuid();
        }

        public virtual DamageEvaluation EvaluateDamage(DamageTarget target)
        {
            return default;
        }
    }
}