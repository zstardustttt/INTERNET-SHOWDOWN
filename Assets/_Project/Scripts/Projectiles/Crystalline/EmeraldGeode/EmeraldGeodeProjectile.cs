using Game.Core.Projectiles;
using UnityEngine;

namespace Game.Projectiles.Crystalline.EmeraldGeode
{
    public class EmeraldGeodeProjectile : PredictableProjectile
    {
        [Header("Objects")]
        public ProjectileCollision collision;

        [Header("Properties")]
        public float flySpeed;

        protected override void OnSpawned()
        {
            collision.onCollision.AddListener(OnCollision);

            PredictSpawn(1, (previous, current) =>
            {
                collision.CheckCollisionBetweenTwoPoints(previous.position, current.position);
            });
        }

        protected override void OnDestroyed()
        {
            collision.onCollision.RemoveAllListeners();
        }

        private void OnCollision(Vector3 point, Vector3 normal, Collider other)
        {
            DestroyProjectile();
        }

        public override ProjectilePredictionData Predict(float timePassed)
        {
            var velocity = flySpeed * transform.forward;
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