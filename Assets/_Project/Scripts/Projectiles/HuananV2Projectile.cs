using Game.Core.Maps;
using Game.Core.Projectiles;
using Game.Player;
using Mirror;
using UnityEngine;

namespace Game.Projectiles
{
    public class HuananV2Projectile : PredictableProjectile
    {
        public ProjectileCollision collision;
        public DamageDealer explosionPrefab;
        public float speed;
        public float rotationSpeed;
        public Transform visual;

        public override void Init()
        {
            collision.onCollision.AddListener(ProjectileCollision);

            var prediction = Predict(SpawnDelay);
            collision.CheckCollisionBetweenTwoPoints(_spawnPosition, prediction.position);
            rb.linearVelocity = transform.forward * speed;
        }

        private void ProjectileCollision(Vector3 point, Vector3 normal, Collider other)
        {
            var explosion = Instantiate(explosionPrefab.gameObject, point, Quaternion.identity, new InstantiateParameters()
            {
                scene = MapLoader.loadedMap.scene
            });
            explosion.GetComponent<DamageDealer>().owner = _owner;
            NetworkServer.Spawn(explosion);
            NetworkServer.Destroy(gameObject);
        }

        protected override void OnDealerHit(DamageDealer dealer, PlayerBase player, float damage) { }

        protected override void OnUpdate()
        {
            visual.Rotate(transform.forward, rotationSpeed * Time.deltaTime, Space.World);
        }

        public override ProjectilePredictionData Predict(float timePassed)
        {
            var velocity = speed * transform.forward;
            var predictedPos = _spawnPosition + velocity * timePassed;

            return new()
            {
                position = predictedPos,
                rotation = _spawnRotation,
                velocity = velocity,
            };
        }
    }
}