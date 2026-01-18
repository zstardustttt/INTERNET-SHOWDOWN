using Game.Core.Maps;
using Game.Core.Projectiles;
using Game.Player;
using Mirror;
using UnityEngine;

namespace Game.Projectiles
{
    public class TeslaCocktailProjectile : PredictableProjectile
    {
        public ProjectileCollision collision;
        public DamageDealer fieldPrefab;
        public DamageDealer playerFieldPrefab;
        public float speed;
        public float gravityForce;
        public float activateGravityAfter;
        public int spawnCheckIterations;

        public override void Init()
        {
            collision.onCollision.AddListener(OnCollision);

            var startCastPoint = _spawnPosition;
            for (int i = spawnCheckIterations; i >= 1; i--)
            {
                var prediction = Predict(SpawnDelay / i);
                collision.CheckCollisionBetweenTwoPoints(startCastPoint, prediction.position);
                _spawnPosition = prediction.position;
            }

            rb.linearVelocity = transform.forward * speed;
        }

        private void OnCollision(Vector3 point, Collider other)
        {
            var field = Instantiate(fieldPrefab.gameObject, point, Quaternion.identity, new InstantiateParameters()
            {
                scene = MapLoader.loadedMap.scene
            });
            field.GetComponent<DamageDealer>().owner = _owner;
            NetworkServer.Spawn(field);
            NetworkServer.Destroy(gameObject);
        }

        public override ProjectilePredictionData Predict(float timePassed)
        {
            var flyingVelocity = speed * transform.forward;
            var gravityVelocity = gravityForce * Mathf.Max(timePassed - activateGravityAfter, 0f);
            var velocity = flyingVelocity - Vector3.up * gravityVelocity;

            var predictedPos = _spawnPosition + velocity * timePassed;

            return new()
            {
                position = predictedPos,
                rotation = _spawnRotation,
                velocity = velocity
            };
        }

        protected override void OnDealerHit(DamageDealer dealer, PlayerBase player, float damage)
        {
            var field = Instantiate(playerFieldPrefab.gameObject, player.transform.position, Quaternion.identity, new InstantiateParameters()
            {
                scene = MapLoader.loadedMap.scene
            });
            field.GetComponent<DamageDealer>().owner = _owner;
            field.GetComponent<TeslaCocktailPlayerField>().player = player;
            NetworkServer.Spawn(field);
            NetworkServer.Destroy(gameObject);
        }

        protected override void OnUpdate()
        {
            if (!NetworkServer.active) return;

            if (_lifetime <= activateGravityAfter) return;
            rb.linearVelocity -= gravityForce * Time.deltaTime * Vector3.up;
        }
    }
}