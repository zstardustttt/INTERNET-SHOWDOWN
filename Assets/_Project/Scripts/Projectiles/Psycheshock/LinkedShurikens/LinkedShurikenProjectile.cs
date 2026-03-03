using Game.Core.Maps;
using Game.Core.Projectiles;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Projectiles.Psycheshock.LinkedShurikens
{
    public class LinkedShurikenProjectile : PredictableProjectile
    {
        [Header("Objects")]
        public ProjectileCollision collision;
        public Transform visualToRotate;
        public AudioSource flyAudioSource;
        public AudioSource collideAudioSource;

        [Header("Properties")]
        public float startFlySpeed;
        public float minFlySpeed;
        public float maxLifetime;
        public float flyAudioCenterPitch;
        public float visualRotationFactor;
        public UnityEvent<LinkedShurikenProjectile> onDestroy = new();

        [HideInInspector, SyncVar] public float collideAudioPitch;

        private float _flySpeed;
        private Vector3 _flyDirection;

        protected override void OnSpawned()
        {
            _flySpeed = startFlySpeed;
            _flyDirection = transform.forward;

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

        void OnDestroy()
        {
            if (!NetworkServer.active) return;
            onDestroy.Invoke(this);
        }

        protected override void OnUpdate()
        {
            visualToRotate.Rotate(Vector3.up, rb.linearVelocity.magnitude * visualRotationFactor * Time.deltaTime);
            flyAudioSource.pitch = _flySpeed / startFlySpeed * flyAudioCenterPitch;
            if (!NetworkServer.active) return;

            rb.linearVelocity = _flyDirection * _flySpeed;
            if (lifetime >= maxLifetime) DestroyProjectile();
        }

        private void OnCollision(Vector3 point, Vector3 normal, Collider other)
        {
            var directionToClosest = Vector3.zero;
            var closestDistance = 2000f;
            foreach (var (_, player) in MapLoader.loadedMap.players)
            {
                if (player.deathModule.Dead || player == author) continue;
                var distance = Vector3.Distance(player.transform.position, transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    directionToClosest = (player.transform.position - transform.position).normalized;
                }
            }

            _flyDirection = (Vector3.Reflect(_flyDirection, normal) / 2f + directionToClosest).normalized;
            _flySpeed = Mathf.Max(_flySpeed / 3f, minFlySpeed);

            transform.forward = _flyDirection;
            RpcPlayCollisionAudio();
        }

        [ClientRpc]
        public void RpcPlayCollisionAudio()
        {
            collideAudioSource.pitch = collideAudioPitch;
            collideAudioSource.Play();
        }

        public override ProjectilePredictionData Predict(float timePassed)
        {
            var velocity = _flySpeed * _flyDirection;
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