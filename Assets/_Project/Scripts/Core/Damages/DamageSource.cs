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

    public class DamageSource : HitListener, IBroadcastReceiver<SetupDamageSourceBroadcast>
    {
        public Guid Guid { get; private set; }

        public bool canDamageFamily;
        public UnityEvent<DamageEvent> onWishDamage;
        public UnityEvent<DamageEvent> onDamage;

        [HideInInspector] public PlayerBase author;
        [HideInInspector] public Guid family;

        private void Awake()
        {
            Guid = Guid.NewGuid();
        }

        public virtual DamageEvaluation EvaluateDamage(DamageTarget target)
        {
            return default;
        }

        public void Receive(SetupDamageSourceBroadcast broadcast)
        {
            family = broadcast.family;
            author = broadcast.author;
        }
    }
}