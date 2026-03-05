using Game.Core.Broadcast;
using Game.Core.Damages;
using Game.Core.Maps;
using Game.Core.Projectiles;
using Mirror;
using UnityEngine;

namespace Game.Projectiles.Crystalline.EmeraldGeode
{
    public class EmeraldGeodeSpawnerProjectile : PredictableProjectile
    {
        [Header("Objects")]
        public ProjectileCollision collision;
        public GameObject emeraldGeodePrefab;

        [Header("Properties")]
        public float speed;
        public float gravityAcceleration;
        public float activateGravityAfter;

        protected override void OnSpawned()
        {
            collision.onCollision.AddListener(OnCollision);
            PredictSpawn(4, (previous, current) =>
            {
                collision.CheckCollisionBetweenTwoPoints(previous.position, current.position);
            });
        }

        protected override void OnDestroyed()
        {
            collision.onCollision.RemoveAllListeners();
        }

        protected override void OnUpdate()
        {
            if (!NetworkServer.active) return;

            if (lifetime <= activateGravityAfter) return;
            rb.linearVelocity -= gravityAcceleration * Time.deltaTime * Vector3.up;
        }

        private void OnCollision(Vector3 point, Vector3 normal, Collider other)
        {
            var emeraldGeode = MapLoader.NetworkSpawnOnMap(emeraldGeodePrefab, point, Quaternion.FromToRotation(Vector3.up, normal));
            emeraldGeode.BroadcastOnChildren(new SetupDamageSourceBroadcast()
            {
                author = author,
                family = author.healthModule.family,
            });

            DestroyProjectile();
        }

        public override ProjectilePredictionData Predict(float timePassed)
        {
            var timePassedSinceStartedAccelerating = Mathf.Max(timePassed - activateGravityAfter, 0f);

            var flyingVelocity = speed * transform.forward;
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