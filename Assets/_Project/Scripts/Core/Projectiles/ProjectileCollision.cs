using UnityEngine;
using UnityEngine.Events;

namespace Game.Core.Projectiles
{
    [RequireComponent(typeof(Projectile))]
    public abstract class ProjectileCollision : MonoBehaviour
    {
        public bool active = true;
        public abstract Collider Collider { get; }
        public Projectile projectile;
        public LayerMask collisionLayerMask;
        public UnityEvent<Vector3, Vector3, Collider> onCollision = new();

        protected virtual void OnValidate()
        {
            projectile = GetComponent<Projectile>();
            projectile.rb.includeLayers = collisionLayerMask;
            projectile.rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        public void CheckLinecastBetweenTwoPoints(Vector3 p1, Vector3 p2)
        {
            if (!active) return;
            if (!Physics.Linecast(p1, p2, out var hit, collisionLayerMask)) return;
            if (hit.collider.gameObject == gameObject) return;
            onCollision.Invoke(hit.point, hit.normal, hit.collider);
        }

        public void CheckCollisionBetweenTwoPoints(Vector3 p1, Vector3 p2)
        {
            if (!active) return;
            CheckCollisionBetweenTwoPointsInside(p1, p2);
        }

        protected abstract void CheckCollisionBetweenTwoPointsInside(Vector3 p1, Vector3 p2);

        private void OnCollisionEnter(Collision collision)
        {
            if (!active) return;
            var firstContact = collision.contacts[0];
            onCollision.Invoke(firstContact.point, firstContact.normal, collision.collider);
        }
    }
}