using System;
using Game.Core.Damages.Events;
using Game.Core.Hits;
using Game.Player;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Core.Damages
{
    public class DamageSource : HitListener
    {
        public bool canDamageFamily;
        public UnityEvent<DamageEvent> onDamage;

        [HideInInspector] public Guid family;
        [HideInInspector] public PlayerBase author;

        public virtual Damage? EvaluateDamage(DamageTarget target)
        {
            return null;
        }
    }
}