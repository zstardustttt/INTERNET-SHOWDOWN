using UnityEngine;
using UnityEngine.Events;

namespace Game.Core.Projectiles
{
    public enum CollisionMethod
    {
        Enter,
        Stay
    }

    [RequireComponent(typeof(Projectile))]
    public abstract class ProjectileCollision : MonoBehaviour
    {
        public bool active = true;
        public abstract Collider Collider { get; }
        public Projectile projectile;
        public LayerMask collisionLayerMask;
        public CollisionMethod collisionMethod;
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

        private void OnCollisionStay(Collision collision)
        {
            if (collisionMethod != CollisionMethod.Stay) return;
            InternalOnCollision(collision);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collisionMethod != CollisionMethod.Enter) return;
            InternalOnCollision(collision);
        }

        private void InternalOnCollision(Collision collision)
        {
            if (!active) return;

            foreach (var contact in collision.contacts)
            {
                onCollision.Invoke(contact.point, contact.normal, collision.collider);
            }
        }
    }
}