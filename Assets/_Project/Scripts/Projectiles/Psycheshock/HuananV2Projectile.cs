using Game.Core.Damages;
using Game.Core.Maps;
using Game.Core.Projectiles;
using Mirror;
using UnityEngine;

namespace Game.Projectiles.Psycheshock
{
    public class HuananV2Projectile : PredictableProjectile
    {
        [Header("Objects")]
        public GameObject explosionPrefab;
        public Transform visual;
        public DamageSource mainDamage;

        [Header("Properties")]
        public float speed;
        public float rotationSpeed;

        public override void OnStartServer()
        {
            // TODO: could be replaced using SendMessage API
            mainDamage.author = author;
            mainDamage.family = author.healthModule.family;
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

        protected override void OnCollision(Vector3 point, Vector3 normal, Collider other)
        {
            var explosion = Instantiate(explosionPrefab, point, Quaternion.identity, new InstantiateParameters()
            {
                scene = MapLoader.loadedMap.scene
            }).GetComponent<DamageSource>();
            explosion.author = author;
            explosion.family = author.healthModule.family;
            NetworkServer.Spawn(explosion.gameObject);
            DestroyProjectile();
        }
    }
}