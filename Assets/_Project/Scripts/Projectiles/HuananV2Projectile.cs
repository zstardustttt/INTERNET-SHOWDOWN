using Game.Core.Damage;
using Game.Core.Maps;
using Game.Core.Projectiles;
using Game.Player;
using Mirror;
using UnityEngine;

namespace Game.Projectiles
{
    public class HuananV2Projectile : PredictableProjectile
    {
        public DamageDealer explosionPrefab;
        public float speed;
        public float rotationSpeed;
        public Transform visual;

        protected override void OnDealerHit(DamageDealer dealer, DamageReceiver receiver, float damage) { }

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

        protected override void OnCollision(Vector3 point, Vector3 normal, Collider other)
        {
            var explosion = Instantiate(explosionPrefab.gameObject, point, Quaternion.identity, new InstantiateParameters()
            {
                scene = MapLoader.loadedMap.scene
            });
            explosion.GetComponent<DamageDealer>().owner = _owner;
            NetworkServer.Spawn(explosion);
            DestroyProjectile();
        }
    }
}