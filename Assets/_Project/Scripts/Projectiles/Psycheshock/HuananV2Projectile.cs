using Game.Core.Broadcast;
using Game.Core.Damages;
using Game.Core.Maps;
using Game.Core.Projectiles;
using UnityEngine;

namespace Game.Projectiles.Psycheshock
{
    public class HuananV2Projectile : PredictableProjectile
    {
        [Header("Objects")]
        public GameObject explosionPrefab;
        public GameObject shockEffectPrefab;
        public Transform visual;
        public DamageSource mainDamage;
        public ProjectileCollision collision;

        [Header("Properties")]
        public float speed;
        public float rotationSpeed;

        protected override void OnSpawned()
        {
            collision.onCollision.AddListener((point, _, _) => Explode(point));
            mainDamage.onDamage.AddListener((damageEvent) =>
            {
                var position = damageEvent.target.hitEntity.Collider.bounds.center;
                MapLoader.NetworkSpawnOnMap(shockEffectPrefab, position, Quaternion.identity);
            });

            PredictSpawn(1, (previousPrediction, prediction) =>
            {
                collision.CheckCollisionBetweenTwoPoints(previousPrediction.position, prediction.position);
            });
        }

        protected override void OnDestroyed()
        {
            collision.onCollision.RemoveAllListeners();
            mainDamage.onDamage.RemoveAllListeners();
        }

        protected override void OnUpdate()
        {
            visual.Rotate(transform.forward, rotationSpeed * Time.deltaTime, Space.World);
        }

        public override ProjectilePredictionData Predict(float timePassed)
        {
            var velocity = speed * transform.forward;
            var predictedPos = spawnPosition + velocity * timePassed;

            return new()
            {
                position = predictedPos,
                rotation = spawnRotation,
                velocity = velocity,
            };
        }

        public void Explode(Vector3 point)
        {
            var explosion = MapLoader.NetworkSpawnOnMap(explosionPrefab, point, Quaternion.identity);
            explosion.BroadcastOnChildren(new SetupDamageSourceBroadcast()
            {
                family = author.healthModule.family,
                author = author
            });

            DestroyProjectile();
        }
    }
}