using Game.Core.Projectiles;
using UnityEngine;

namespace Game.Projectiles
{
    [RequireComponent(typeof(SphereCollider))]
    public class SphereProjectileCollision : ProjectileCollision
    {
        public SphereCollider coll;

        protected override void OnValidate()
        {
            base.OnValidate();
            coll = GetComponent<SphereCollider>();
        }

        public override void CheckCollisionBetweenTwoPoints(Vector3 p1, Vector3 p2)
        {
            var delta = p2 - p1;
            var hits = Physics.SphereCastAll(p1, coll.radius, delta.normalized, delta.magnitude, collisionLayerMask);

            foreach (var hit in hits)
            {
                if (hit.collider == coll || hit.point == Vector3.zero) continue;
                onCollision.Invoke(hit.point, hit.normal, hit.collider);
            }
        }
    }
}