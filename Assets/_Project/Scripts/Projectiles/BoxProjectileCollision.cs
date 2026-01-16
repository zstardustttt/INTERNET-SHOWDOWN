using Game.Core.Projectiles;
using UnityEngine;

namespace Game.Projectiles
{
    [RequireComponent(typeof(BoxCollider))]
    public class BoxProjectileCollision : ProjectileCollision
    {
        public BoxCollider coll;
        protected override void OnValidate()
        {
            base.OnValidate();
            coll = GetComponent<BoxCollider>();
        }

        public override void CheckCollisionBetweenTwoPoints(Vector3 p1, Vector3 p2)
        {
            var delta = p2 - p1;
            if (!Physics.BoxCast(p1, coll.size / 2f, delta.normalized, out var hit, transform.rotation, delta.magnitude, collisionLayerMask))
                return;

            if (hit.collider == coll) return;
            onCollision.Invoke(hit.point, hit.collider);
        }
    }
}