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
        public double spawnTime;

        public abstract ProjectilePredictionData Predict(float timePassed);
    }
}