using System;
using Game.Core.Broadcast;
using Game.Core.Damages.Events;
using Game.Core.Hits;
using Game.Player;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Core.Damages
{
    public struct SetupDamageSourceBroadcast
    {
        public Guid family;
        public PlayerBase author;
    }

    public class DamageSource : HitListener, IBroadcastReceiver<SetupDamageSourceBroadcast>
    {
        public bool canDamageFamily;
        public UnityEvent<DamageEvent> onWishDamage;
        public UnityEvent<DamageEvent> onDamage;

        [HideInInspector] public Guid family;
        [HideInInspector] public PlayerBase author;

        public virtual Damage? EvaluateDamage(DamageTarget target)
        {
            return null;
        }

        public void Receive(SetupDamageSourceBroadcast broadcast)
        {
            family = broadcast.family;
            author = broadcast.author;
        }
    }
}