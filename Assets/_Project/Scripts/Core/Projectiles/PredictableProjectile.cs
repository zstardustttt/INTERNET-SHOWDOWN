using Game.Core.Events;
using Game.Events.HitWatcher;
using Game.Player;
using Mirror;
using UnityEngine;

namespace Game.Core.Projectiles
{
    public struct ProjectilePredictionData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
    }

    public abstract class PredictableProjectile : Projectile
    {
        public double SpawnTime { get; private set; }
        public float SpawnDelay { get; private set; }

        [Header("Prediction Debug")]
        public int predictionDebugIterations = 1;
        public float predictionDebugTimePassed;

        public abstract ProjectilePredictionData Predict(float timePassed);

        public static T Spawn<T>(T prefab, PlayerBase owner, Vector3 position, Quaternion rotation, double spawnTime, int checkIterations) where T : PredictableProjectile
        {
            var projectile = Spawn(prefab, owner, position, rotation);
            projectile.SpawnTime = spawnTime;
            projectile.SpawnDelay = (float)(NetworkTime.time - spawnTime);

            var startCastPoint = position;
            var deltaTime = projectile.SpawnDelay / checkIterations;
            for (int i = 0; i < checkIterations; i++)
            {
                var prediction = projectile.Predict(deltaTime * i);
                foreach (var dealer in projectile.damageDealers)
                {
                    EventBus<RequestTwoPointsDealerCheck>.Invoke(new()
                    {
                        dealer = dealer,
                        point1 = startCastPoint,
                        point2 = prediction.position,
                    });
                }

                startCastPoint = prediction.position;
            }

            projectile.transform.position = startCastPoint;
            return projectile;
        }

        private void OnDrawGizmosSelected()
        {
            var deltaTime = predictionDebugTimePassed / predictionDebugIterations;
            var lastStartPoint = _spawnPosition;
            for (int i = 0; i < predictionDebugIterations; i++)
            {
                var prediction = Predict(deltaTime * i);

                Gizmos.color = Color.red;
                Gizmos.DrawLine(lastStartPoint, prediction.position);

                Gizmos.color = Color.blue;
                Gizmos.DrawLine(prediction.position, prediction.position + prediction.rotation * Vector3.up);

                Gizmos.color = Color.green;
                Gizmos.DrawRay(prediction.position, prediction.velocity * deltaTime);

                lastStartPoint = prediction.position;
            }
        }
    }
}