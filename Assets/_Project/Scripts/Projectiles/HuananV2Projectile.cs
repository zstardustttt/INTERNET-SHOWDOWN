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
            rb.linearVelocity = transform.forward * speed;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!NetworkServer.active) return;
            var explosion = Instantiate(explosionPrefab.gameObject, collision.contacts[0].point, Quaternion.identity, new InstantiateParameters()
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