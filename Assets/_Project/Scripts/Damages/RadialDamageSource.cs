using Game.Core.Damages;
using UnityEngine;

namespace Game.Damages
{
    public class RadialDamageSource : DamageSource
    {
        public Vector3 centerOffset;
        public DamageType damageType;

        [Space(9)]
        public float outerRadius;
        public float outerDamageAmount;

        [Space(9)]
        public float innerRadius;
        public float innerDamageAmount;

        public override Damage? EvaluateDamage(DamageTarget target)
        {
            var center = transform.position + centerOffset;
            var distance = Vector3.Distance(target.hitEntity.Collider.bounds.center, center);
            if (distance <= innerRadius)
                return new(author, damageType, innerDamageAmount);

            var damageAmount = Mathf.Lerp(innerDamageAmount, outerDamageAmount, Mathf.InverseLerp(innerRadius, outerRadius, distance));
            return new(author, damageType, damageAmount);
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