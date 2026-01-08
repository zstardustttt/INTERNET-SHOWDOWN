using Game.Player;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Core.Projectiles
{
    [RequireComponent(typeof(Collider))]
    public abstract class DamageDealer : MonoBehaviour
    {
        public PlayerBase owner;
        public UnityEvent<PlayerBase> OnDamageDealt = new();
        public abstract float EvaluateDamage(PlayerBase player);
    }
}