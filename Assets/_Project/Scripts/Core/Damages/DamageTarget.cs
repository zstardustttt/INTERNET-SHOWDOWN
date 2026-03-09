using Game.Core.Damages.Events;
using Game.Core.Hits;
using UnityEngine.Events;

namespace Game.Core.Damages
{
    public class DamageTarget : HitListener
    {
        public TeamReference teamReference;
        public UnityEvent<DamageEvent> onWishDamage;
        public UnityEvent<DamageEvent> onDamage;

        public virtual bool ApplyDamage(Damage damage)
        {
            return true;
        }
    }
}