using Game.Core.Projectiles;
using Mirror;
using UnityEngine;

namespace Game.Projectiles.Crystalline.EmeraldGeode
{
    public class EmeraldGeodeProjectile : PredictableProjectile
    {
        [Header("Objects")]
        public ProjectileCollision collision;
        public Transform visual;

        [Header("Properties")]
        public float maxLifetime;
        public float flySpeed;
        public float gravityAcceleration;
        public float activateGravityAfter;

        [SyncVar] private Vector3 _currentDirection;

        protected override void OnSpawned()
        {
            collision.onCollision.AddListener(OnCollision);
        }

        protected override void OnDestroyed()
        {
            collision.onCollision.RemoveAllListeners();
        }

        protected override void OnUpdate()
        {
            if (_currentDirection != Vector3.zero) visual.forward = _currentDirection;

            if (!NetworkServer.active) return;

            if (lifetime > maxLifetime)
            {
                DestroyProjectile();
                return;
            }

            var gravity = gravityAcceleration * Mathf.Max(0f, lifetime - activateGravityAfter);
            rb.linearVelocity = transform.forward * flySpeed + gravity * Vector3.down;
            _currentDirection = rb.linearVelocity.normalized;
        }

        private void OnCollision(Vector3 point, Vector3 normal, Collider other)
        {
            DestroyProjectile();
        }

        public override ProjectilePredictionData Predict(float timePassed)
        {
            var timePassedSinceStartedAccelerating = Mathf.Max(timePassed - activateGravityAfter, 0f);

            var flyingVelocity = flySpeed * transform.forward;
            var gravityVelocity = gravityAcceleration * timePassedSinceStartedAccelerating * Vector3.down;
            var velocity = flyingVelocity + gravityVelocity;

            var predictedPos = spawnPosition + flyingVelocity * timePassed + 0.5f * timePassedSinceStartedAccelerating * gravityVelocity;

            return new()
            {
                position = predictedPos,
                rotation = spawnRotation,
                velocity = velocity
            };
        }
    }
}