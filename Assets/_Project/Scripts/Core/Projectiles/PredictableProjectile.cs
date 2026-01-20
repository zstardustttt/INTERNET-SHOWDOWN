using Game.Core.Events;
using Game.Events.HitWatcher;
using Game.Player;
using Mirror;
using Unity.VisualScripting;
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

        public void SetupPrediction(double spawnTime, int checkIterations)
        {
            SpawnTime = spawnTime;
            SpawnDelay = (float)(NetworkTime.time - spawnTime);
            _lifetime = SpawnDelay;

            var finalPrediction = Predict(SpawnDelay);
            transform.SetPositionAndRotation(finalPrediction.position, finalPrediction.rotation);
            rb.linearVelocity = finalPrediction.velocity;

            var previousPrediction = new ProjectilePredictionData()
            {
                position = _spawnPosition,
                rotation = _spawnRotation,
                velocity = rb.linearVelocity,
            };

            var deltaTime = SpawnDelay / checkIterations;
            for (int i = 1; i <= checkIterations; i++)
            {
                var prediction = i == checkIterations ? finalPrediction : Predict(deltaTime * i);
                foreach (var dealer in damageDealers)
                {
                    EventBus<RequestTwoPointsDealerCheck>.Invoke(new()
                    {
                        dealer = dealer,
                        point1 = previousPrediction.position,
                        point2 = prediction.position,
                    });
                }

                if (collision)
                    collision.CheckCollisionBetweenTwoPoints(previousPrediction.position, prediction.position);

                previousPrediction = prediction;
            }
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