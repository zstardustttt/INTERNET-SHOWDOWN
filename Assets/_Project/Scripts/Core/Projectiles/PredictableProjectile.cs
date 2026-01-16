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

        public abstract ProjectilePredictionData Predict(float timePassed);

        public static T Spawn<T>(T prefab, PlayerBase owner, Vector3 position, Quaternion rotation, double spawnTime) where T : PredictableProjectile
        {
            var projectile = Spawn(prefab, owner, position, rotation);
            projectile.SpawnTime = spawnTime;
            var prediction = projectile.Predict((float)(NetworkTime.time - spawnTime));

            foreach (var dealer in projectile.damageDealers)
            {
                EventBus<RequestTwoPointsDealerCheck>.Invoke(new()
                {
                    dealer = dealer,
                    point1 = position,
                    point2 = prediction.position,
                });
            }
            projectile.transform.position = prediction.position;

            return projectile;
        }
    }
}