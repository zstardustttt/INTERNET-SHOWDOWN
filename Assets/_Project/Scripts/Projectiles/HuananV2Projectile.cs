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
        public Rigidbody rb;
        public BoxCollider bc;
        public float speed;
        public float rotationSpeed;
        public Transform visual;

        public override void Init()
        {
            var prediction = Predict((float)(NetworkTime.time - spawnTime));
            var distance = (prediction.position - spawnPosition).magnitude;
            if (Physics.BoxCast(spawnPosition, bc.size / 2f, transform.forward, out var hit, transform.rotation, distance, LayerMask.GetMask("Enviroment")))
            {
                ProjectileCollision(hit.point);
                return;
            }

            rb.linearVelocity = transform.forward * speed;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!NetworkServer.active) return;
            ProjectileCollision(collision.contacts[0].point);
        }

        private void ProjectileCollision(Vector3 point)
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
            var predictedPos = spawnPosition + velocity * timePassed;

            return new()
            {
                position = predictedPos,
                rotation = spawnRotation,
                velocity = velocity,
            };
        }
    }
}