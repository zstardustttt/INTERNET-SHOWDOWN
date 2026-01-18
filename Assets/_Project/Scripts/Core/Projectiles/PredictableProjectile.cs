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

        public abstract ProjectilePredictionData Predict(float timePassed);

        public static T Spawn<T>(T prefab, PlayerBase owner, Vector3 position, Quaternion rotation, double spawnTime, int checkIterations) where T : PredictableProjectile
        {
            var projectile = Spawn(prefab, owner, position, rotation);
            projectile.SpawnTime = spawnTime;
            projectile.SpawnDelay = (float)(NetworkTime.time - spawnTime);

            var prediction = projectile.Predict(projectile.SpawnDelay);

            var startCastPoint = position;
            for (int i = checkIterations; i >= 1; i--)
            {
                var localPrediction = projectile.Predict(projectile.SpawnDelay / i);
                foreach (var dealer in projectile.damageDealers)
                {
                    EventBus<RequestTwoPointsDealerCheck>.Invoke(new()
                    {
                        dealer = dealer,
                        point1 = startCastPoint,
                        point2 = localPrediction.position,
                    });
                }

                startCastPoint = localPrediction.position;
            }

            projectile.transform.position = prediction.position;
            return projectile;
        }
    }
}