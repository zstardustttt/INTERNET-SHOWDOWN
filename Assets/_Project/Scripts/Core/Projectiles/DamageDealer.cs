using Game.Player;
using UnityEngine;

namespace Game.Core.Projectiles
{
    [RequireComponent(typeof(Collider))]
    public abstract class DamageDealer : MonoBehaviour
    {
        public PlayerBase owner;
        public abstract float EvaluateDamage(PlayerBase player);
    }
}