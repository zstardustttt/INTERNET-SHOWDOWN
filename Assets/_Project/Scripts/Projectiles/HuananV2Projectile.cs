using Game.Core.Events;
using Game.Core.Projectiles;
using Game.Events.HitWatcher;
using Game.Player;
using Mirror;
using UnityEngine;

namespace Game.Projectiles
{
    public class HuananV2Projectile : PredictableProjectile
    {
        public Rigidbody rb;
        public BoxCollider bc;
        public float speed;
        public float rotationSpeed;
        public Transform visual;

        public override void Init()
        {
            rb.linearVelocity = transform.forward * speed;
            var prediction = Predict((float)(NetworkTime.time - spawnTime));

            foreach (var dealer in damageDealers)
            {
                EventBus<RequestTwoPointsDealerCheck>.Invoke(new()
                {
                    dealer = dealer,
                    point1 = spawnPosition,
                    point2 = prediction.position,
                });
            }
            transform.position = prediction.position;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!NetworkServer.active) return;
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