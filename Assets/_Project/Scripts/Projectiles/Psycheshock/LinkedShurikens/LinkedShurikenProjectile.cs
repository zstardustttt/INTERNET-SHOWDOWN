using Game.Core.Damages;
using Game.Core.Hits;
using Game.Core.Maps;
using Game.Core.Projectiles;
using Game.Damages;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Projectiles.Psycheshock.LinkedShurikens
{
    public class LinkedShurikenProjectile : PredictableProjectile
    {
        [Header("Objects")]
        public ProjectileCollision collision;
        public HitEntity hitEntity;
        public BasicDamageSource mainDamage;
        public Transform visualToRotate;
        public AudioSource flyAudioSource;
        public AudioSource collideAudioSource;
        public ParticleSystem collideParticleEffect;
        public GameObject shockEffectPrefab;

        [Header("Properties")]
        public float startFlySpeed;
        public float maxLifetime;
        public float visualRotationFactor;
        public UnityEvent onCollide = new();
        public UnityEvent onDestroy = new();

        private bool _collided;

        [HideInInspector] public float collideAudioPitch;

        protected override void OnSpawned()
        {
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
            onDestroy.Invoke();
        }

        protected override void OnUpdate()
        {
            if (!_collided) visualToRotate.Rotate(Vector3.up, visualRotationFactor * Time.deltaTime);

            if (!NetworkServer.active) return;
            if (lifetime >= maxLifetime) DestroyProjectile();
        }

        private void OnCollision(Vector3 point, Vector3 normal, Collider other)
        {
            if (_collided) return;

            var dot = Vector3.Dot(transform.forward, normal);
            if (dot > -0.5f && dot < 0.5f)
            {
                transform.forward = (transform.forward - normal) / 2f;
            }

            transform.position = point + normal * 0.1f;
            rb.linearVelocity = Vector3.zero;
            hitEntity.enabled = false;

            onCollide.Invoke();
            RpcOnCollide(collideAudioPitch);
            _collided = true;
        }

        [ClientRpc]
        public void RpcOnCollide(float pitch)
        {
            collideAudioSource.pitch = pitch;
            collideAudioSource.Play();
            collideParticleEffect.Play();
            flyAudioSource.Stop();

            _collided = true;
        }

        public override ProjectilePredictionData Predict(float timePassed)
        {
            var velocity = transform.forward * startFlySpeed;
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