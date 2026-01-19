using Game.Core.Projectiles;
using UnityEngine;

namespace Game.Projectiles
{
    [RequireComponent(typeof(SphereCollider))]
    public class SphereProjectileCollision : ProjectileCollision
    {
        public SphereCollider coll;

        public override Bounds Bounds => coll.bounds;

        protected override void OnValidate()
        {
            base.OnValidate();
            coll = GetComponent<SphereCollider>();
        }

        public override void CheckCollisionBetweenTwoPoints(Vector3 p1, Vector3 p2)
        {
            var delta = p2 - p1;
            if (!Physics.SphereCast(p1, coll.radius, delta.normalized, out var hit, delta.magnitude, collisionLayerMask))
                return;

            if (hit.collider == coll) return;
            onCollision.Invoke(hit.point, hit.normal, hit.collider);
        }
    }
}