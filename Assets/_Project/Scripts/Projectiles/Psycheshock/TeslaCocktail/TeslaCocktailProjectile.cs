using Game.Core.Broadcast;
using Game.Core.Damages;
using Game.Core.Hits;
using Game.Core.Hits.Events;
using Game.Core.Maps;
using Game.Core.Projectiles;
using Game.Player;
using Mirror;
using UnityEngine;

namespace Game.Projectiles.Psycheshock.TeslaCocktail
{
    public class TeslaCocktailProjectile : PredictableProjectile, IBroadcastReceiver<ProjectileCollisionBroadcast>
    {
        [Header("Objects")]
        public GameObject fieldPrefab;
        public GameObject playerFieldPrefab;
        public HitListener hitListener;

        [Header("Properties")]
        public float speed;
        public float gravityAcceleration;
        public float activateGravityAfter;

        public override void OnStartServer()
        {
            hitListener.onHit.AddListener(OnHit);
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

        private void OnHit(HitEvent hitEvent)
        {
            if (!hitEvent.target.TryGetComponent(out PlayerBase player)) return;
            if (player == author) return;

            var field = Instantiate(playerFieldPrefab, player.transform.position, Quaternion.identity, new InstantiateParameters()
            {
                scene = MapLoader.loadedMap.scene
            });
            field.GetComponent<DamageSource>().author = author;
            field.GetComponent<TeslaCocktailPlayerField>().player = player;
            NetworkServer.Spawn(field);
            DestroyProjectile();
        }

        protected override void OnUpdate()
        {
            if (!NetworkServer.active) return;

            if (lifetime <= activateGravityAfter) return;
            rb.linearVelocity -= gravityAcceleration * Time.deltaTime * Vector3.up;
        }

        public void Receive(ProjectileCollisionBroadcast broadcast)
        {
            var field = Instantiate(fieldPrefab, broadcast.point, Quaternion.identity, new InstantiateParameters()
            {
                scene = MapLoader.loadedMap.scene
            });
            field.transform.up = broadcast.normal;
            field.GetComponent<DamageSource>().author = author;
            NetworkServer.Spawn(field);
            NetworkServer.Destroy(gameObject);
        }
    }
}