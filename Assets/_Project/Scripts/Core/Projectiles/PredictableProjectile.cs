using System;
using Game.Core.Broadcast;
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
        public float SpawnDelay { get; private set; }

        [Header("Prediction Debug")]
        public int predictionDebugIterations = 1;
        public float predictionDebugTimePassed;

        public abstract ProjectilePredictionData Predict(float timePassed);

        public void PredictSpawn(int checkIterations, Action<ProjectilePredictionData, ProjectilePredictionData> onIteration = null)
        {
            SpawnDelay = (float)(NetworkTime.time - spawnTime);
            lifetime = SpawnDelay;

            var finalPrediction = Predict(SpawnDelay);
            transform.SetPositionAndRotation(finalPrediction.position, finalPrediction.rotation);
            if (!rb.isKinematic) rb.linearVelocity = finalPrediction.velocity;

            var previousPrediction = new ProjectilePredictionData()
            {
                position = spawnPosition,
                rotation = spawnRotation,
                velocity = rb.linearVelocity,
            };

            var deltaTime = SpawnDelay / checkIterations;
            for (int i = 1; i <= checkIterations; i++)
            {
                var prediction = i == checkIterations ? finalPrediction : Predict(deltaTime * i);
                onIteration?.Invoke(previousPrediction, prediction);
                previousPrediction = prediction;
            }
        }

        private void OnDrawGizmosSelected()
        {
            var deltaTime = predictionDebugTimePassed / predictionDebugIterations;
            var lastStartPoint = spawnPosition;
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