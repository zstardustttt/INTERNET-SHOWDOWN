using Game.Core.Projectiles;
using UnityEngine;

namespace Game.Projectiles.Collisions
{
    [RequireComponent(typeof(SphereCollider))]
    public class SphereProjectileCollision : ProjectileCollision
    {
        public SphereCollider coll;

        public override Collider Collider => coll;

        protected override void OnValidate()
        {
            base.OnValidate();
            coll = GetComponent<SphereCollider>();
        }

        protected override void CheckCollisionBetweenTwoPointsInside(Vector3 p1, Vector3 p2)
        {
            var delta = p2 - p1;
            var deltaDir = delta.normalized;
            var hits = Physics.SphereCastAll(p1, coll.radius, deltaDir, delta.magnitude, collisionLayerMask);

            foreach (var hit in hits)
            {
                if (hit.collider == coll) continue;

                var point = hit.point;
                var normal = hit.normal;
                if (hit.point == Vector3.zero)
                {
                    point = p1;
                    normal = -deltaDir;
                }

                onCollision.Invoke(point, normal, hit.collider);
            }
        }
    }
}