using System;
using Game.Core.Damages.Events;
using Game.Core.Hits;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Core.Damages
{
    public class DamageTarget : HitListener
    {
        public UnityEvent<DamageEvent> onDamage;

        [HideInInspector] public Guid family;

        public virtual bool ApplyDamage(Damage damage)
        {
            return true;
        }
    }
}