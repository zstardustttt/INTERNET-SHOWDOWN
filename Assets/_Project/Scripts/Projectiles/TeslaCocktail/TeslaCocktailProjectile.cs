using Game.Core.Maps;
using Game.Core.Projectiles;
using Game.Player;
using Mirror;
using UnityEngine;

namespace Game.Projectiles.TeslaCocktail
{
    public class TeslaCocktailProjectile : PredictableProjectile
    {
        public DamageDealer fieldPrefab;
        public DamageDealer playerFieldPrefab;
        public float speed;
        public float gravityAcceleration;
        public float activateGravityAfter;

        protected override void OnCollision(Vector3 point, Vector3 normal, Collider other)
        {
            var field = Instantiate(fieldPrefab.gameObject, point, Quaternion.identity, new InstantiateParameters()
            {
                scene = MapLoader.loadedMap.scene
            });
            field.transform.up = normal;
            field.GetComponent<DamageDealer>().owner = _owner;
            NetworkServer.Spawn(field);
            NetworkServer.Destroy(gameObject);
        }

        public override ProjectilePredictionData Predict(float timePassed)
        {
            var timePassedSinceStartedAccelerating = Mathf.Max(timePassed - activateGravityAfter, 0f);

            var flyingVelocity = speed * transform.forward;
            var gravityVelocity = gravityAcceleration * timePassedSinceStartedAccelerating * Vector3.down;
            var velocity = flyingVelocity + gravityVelocity;

            var predictedPos = _spawnPosition + flyingVelocity * timePassed + 0.5f * timePassedSinceStartedAccelerating * gravityVelocity;

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
            DestroyProjectile();
        }

        protected override void OnUpdate()
        {
            if (!NetworkServer.active) return;

            if (_lifetime <= activateGravityAfter) return;
            rb.linearVelocity -= gravityAcceleration * Time.deltaTime * Vector3.up;
        }
    }
}