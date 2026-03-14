using Game.Core.Broadcast;
using Game.Core.Damages;
using Game.Core.Hits;
using Game.Core.Hits.Events;
using Game.Core.Maps;
using Game.Core.Player;
using Game.Core.Projectiles;
using Mirror;
using UnityEngine;

namespace Game.Projectiles.Psycheshock.TeslaCocktail
{
    public class TeslaCocktailProjectile : PredictableProjectile
    {
        [Header("Objects")]
        public GameObject fieldPrefab;
        public GameObject playerFieldPrefab;
        public ProjectileCollision collision;
        public HitListener hitListener;

        [Header("Properties")]
        public float speed;
        public float gravityAcceleration;
        public float activateGravityAfter;

        protected override void OnSpawned()
        {
            collision.onCollision.AddListener(OnCollision);
            hitListener.onHit.AddListener(OnHit);

            PredictSpawn(4, (previousPrediction, prediction) =>
            {
                collision.CheckCollisionBetweenTwoPoints(previousPrediction.position, prediction.position);
            });
        }

        protected override void OnDestroyed()
        {
            collision.onCollision.RemoveAllListeners();
            hitListener.onHit.RemoveAllListeners();
        }

        protected override void OnUpdate()
        {
            if (!NetworkServer.active) return;

            if (lifetime <= activateGravityAfter) return;
            rb.linearVelocity -= gravityAcceleration * Time.deltaTime * Vector3.up;
        }

        private void OnHit(HitEvent hitEvent)
        {
            if (!hitEvent.target.transform.root.TryGetComponent(out PlayerCore player)) return;
            if (player == authorReference.author) return;

            var playerField = MapLoader.NetworkSpawnOnMap(playerFieldPrefab, player.transform.position, Quaternion.identity);
            playerField.BroadcastOnGameObject(new SetAuthorBroadcast(authorReference.author));
            playerField.BroadcastOnGameObject(new SetTeamBroadcast(teamReference.team));

            playerField.GetComponent<TeslaCocktailPlayerField>().player = player;
            DestroyProjectile();
        }

        private void OnCollision(Vector3 point, Vector3 normal, Collider other)
        {
            var field = MapLoader.NetworkSpawnOnMap(fieldPrefab, point, Quaternion.FromToRotation(Vector3.up, normal));
            field.BroadcastOnGameObject(new SetAuthorBroadcast(authorReference.author));
            field.BroadcastOnGameObject(new SetTeamBroadcast(teamReference.team));

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