using Game.Core.Damage;
using Game.Core.Projectiles;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Projectiles.LinkedShurikens
{
    public class LinkedShurikenProjectile : PredictableProjectile
    {
        public float flySpeed;
        public UnityEvent<LinkedShurikenProjectile> onDestroy = new();
        public float maxLifetime;

        public override ProjectilePredictionData Predict(float timePassed)
        {
            var velocity = flySpeed * transform.forward;
            var predictedPos = _spawnPosition + velocity * timePassed;

            return new()
            {
                position = predictedPos,
                rotation = _spawnRotation,
                velocity = velocity,
            };
        }

        protected override void OnCollision(Vector3 point, Vector3 normal, Collider other)
        {
            var bounds = collision.Collider.bounds;
            var offset = new Vector3
            (
                normal.x * bounds.extents.x,
                normal.y * bounds.extents.y,
                normal.z * bounds.extents.z
            );
            transform.position = point + offset;
            rb.linearVelocity = Vector3.zero;
        }

        protected override void OnDealerHit(DamageDealer dealer, DamageReceiver receiver, float damage) { }

        protected override void OnUpdate()
        {
            if (!NetworkServer.active) return;
            if (_lifetime >= maxLifetime) DestroyProjectile();
        }

        private void OnDestroy()
        {
            if (!NetworkServer.active) return;
            onDestroy.Invoke(this);
        }
    }
}