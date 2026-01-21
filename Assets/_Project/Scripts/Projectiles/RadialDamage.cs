using Game.Core.Projectiles;
using Game.Player;
using UnityEngine;

namespace Game.Projectiles
{
    public class RadialDamage : DamageDealer
    {
        public Vector3 centerOffset;
        public float outerRadius;
        public float outerDamage;
        public float innerRadius;
        public float innerDamage;

        public override float EvaluateDamage(PlayerBase player)
        {
            var center = transform.position + centerOffset;
            var distance = Vector3.Distance(player.transform.position, center);
            if (distance <= innerRadius)
                return innerDamage;

            return Mathf.Lerp(innerDamage, outerDamage, Mathf.InverseLerp(innerRadius, outerRadius, distance));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green - Color.black * 0.6f;
            Gizmos.DrawSphere(transform.position + centerOffset, innerRadius);

            Gizmos.color = Color.red - Color.black * 0.75f;
            Gizmos.DrawSphere(transform.position + centerOffset, outerRadius);
        }
    }
}