using Game.Core.Damages;
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
        public DamageSource mainDamage;
        public Transform visualToRotate;
        public AudioSource flyAudioSource;
        public AudioSource collideAudioSource;
        public AudioSource collideShockAudioSource;
        public ParticleSystem collideParticleEffect;
        public GameObject shockEffectPrefab;

        [Header("Properties")]
        public float startFlySpeed;
        public float minFlySpeed;
        public float maxLifetime;
        public float flyAudioCenterPitch;
        public float flyAudioInitialVolume;
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
            mainDamage.onDamage.AddListener((damageEvent) =>
            {
                var position = damageEvent.target.hitEntity.Collider.bounds.center;
                MapLoader.NetworkSpawnOnMap(shockEffectPrefab, position, Quaternion.identity);
            });

            PredictSpawn(1, (previous, current) =>
            {
                collision.CheckCollisionBetweenTwoPoints(previous.position, current.position);
            });
        }

        protected override void OnDestroyed()
        {
            collision.onCollision.RemoveAllListeners();
            mainDamage.onDamage.RemoveAllListeners();
        }

        void OnDestroy()
        {
            if (!NetworkServer.active) return;
            onDestroy.Invoke(this);
        }

        protected override void OnUpdate()
        {
            visualToRotate.Rotate(Vector3.up, rb.linearVelocity.magnitude * visualRotationFactor * Time.deltaTime);

            flyAudioSource.pitch = Mathf.Max(flyAudioCenterPitch / 2f, _flySpeed / startFlySpeed * flyAudioCenterPitch);
            flyAudioSource.volume = _flySpeed / startFlySpeed * flyAudioInitialVolume;

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
            _flySpeed = Mathf.Max(_flySpeed / Random.Range(3.5f, 5f), minFlySpeed);

            transform.forward = _flyDirection;
            transform.position = point + normal * 0.1f;
            RpcOnCollide();
        }

        [ClientRpc]
        public void RpcOnCollide()
        {
            collideAudioSource.pitch = collideAudioPitch;
            collideAudioSource.Play();
            collideShockAudioSource.Play();
            collideParticleEffect.Play();
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