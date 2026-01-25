using Game.Core.Projectiles;
using UnityEngine;

namespace Game.Projectiles.Collisions
{
    [RequireComponent(typeof(BoxCollider))]
    public class BoxProjectileCollision : ProjectileCollision
    {
        public BoxCollider coll;

        public override Collider Collider => coll;

        protected override void OnValidate()
        {
            base.OnValidate();
            coll = GetComponent<BoxCollider>();
        }

        protected override void CheckCollisionBetweenTwoPointsInside(Vector3 p1, Vector3 p2)
        {
            var delta = p2 - p1;
            var hits = Physics.BoxCastAll(p1, coll.size / 2f, delta.normalized, transform.rotation, delta.magnitude, collisionLayerMask);

            foreach (var hit in hits)
            {
                if (hit.collider == coll) continue;
                onCollision.Invoke(hit.point, hit.normal, hit.collider);
            }
        }
    }
}