using System;
using Game.Core.Broadcast;
using Game.Core.Damages;
using Game.Core.Damages.Events;
using Game.Core.Hits;
using Game.Core.Hits.Events;
using Game.Core.Maps;
using Game.Core.Projectiles;
using Game.Other;
using Game.Player;
using Mirror;
using UnityEngine;

namespace Game.Projectiles.Psycheshock.ShockGerenade
{
    public class ShockGerenadeProjectile : PredictableProjectile, IBroadcastReceiver<ProjectileCollisionBroadcast>
    {
        [Header("Objects")]
        public GameObject explosion;
        public GameObject visual;
        public GameObject localGerenadeVisualPrefab;
        public Transform shakee;

        public HitListener attachHitListener;
        public DamageTarget damageTarget;

        public AudioSource tickAudioSource;
        public AudioSource attachAudioSource;
        public AudioSource detachAudioSource;

        [Header("Properties")]
        public float speed;
        public float secondarySpeed;
        public float gravityAcceleration;
        public float activateGravityAfter;
        public float explodeAfterPrimary;
        public float explodeAfterSecondary;
        public float detachTotalDelta;
        public float collisionRadius;
        public float holdDamage;
        public float visualShakeFrequency;
        public float visualShakeIncreaseRate;

        // client
        private ShockGerenadeLocalVisual _localGerenadeVisual;

        private PlayerBase _attached;
        private Vector3 _previousAttachedPosition;
        private float _collectedAttachedDelta;

        private ShakeGenerator _shakeGenerator;
        private bool _exploded;

        [HideInInspector] public float flySpeed;
        [HideInInspector] public float explodeAfter;
        [HideInInspector, SyncVar] public float sourceSpeedMultiplier;

        private void Awake()
        {
            _shakeGenerator = new()
            {
                shakeFrequency = visualShakeFrequency,
                shakeFalloffSpeed = 0f,
            };
        }

        public override void OnStartServer()
        {
            damageTarget.onDamage.AddListener(OnDamage);
            attachHitListener.onHit.AddListener(OnHit);
        }

        private void OnDamage(DamageEvent damageEvent)
        {
            if (_attached) return;
            var explosionAuthor = damageEvent.damage.type == DamageType.Direct && damageEvent.source.author ? damageEvent.source.author : author;
            Explode(explosionAuthor);
        }

        private void OnDestroy()
        {
            if (!_localGerenadeVisual) return;
            Destroy(_localGerenadeVisual.gameObject);
        }

        private void OnHit(HitEvent hitEvent)
        {
            if (_attached) return;
            if (!hitEvent.target.TryGetComponent(out PlayerBase player)) return;
            if (player == author) return;

            attachHitListener.active = false;

            _attached = player;
            _attached.onDeath.AddListener(Detach);
            _previousAttachedPosition = player.transform.position;

            author.stats.directHits++;
            TargetSpawnVisual(_attached.netIdentity.connectionToClient);
            RpcPlayAttachAudio();
        }

        [ClientRpc]
        private void RpcPlayAttachAudio()
        {
            attachAudioSource.Play();
        }

        [TargetRpc]
        private void TargetSpawnVisual(NetworkConnectionToClient _)
        {
            var player = NetworkClient.localPlayer.GetComponent<PlayerBase>();
            _localGerenadeVisual = Instantiate(localGerenadeVisualPrefab, player.horizontalOrientation).GetComponent<ShockGerenadeLocalVisual>();
            var yOffset = player.motor.Capsule.center.y * 1.1f * Vector3.up;
            _localGerenadeVisual.transform.localPosition = yOffset + Vector3.forward * (player.motor.Capsule.radius + collisionRadius);
            visual.SetActive(false);
            tickAudioSource.spatialBlend = 0f;
            attachAudioSource.spatialBlend = 0f;
        }

        [TargetRpc]
        private void TargetDestroyVisual(NetworkConnectionToClient _)
        {
            LocalDestroyVisual();
        }

        private void LocalDestroyVisual()
        {
            if (!_localGerenadeVisual) return;
            Destroy(_localGerenadeVisual.gameObject);
            visual.SetActive(true);
            tickAudioSource.spatialBlend = 1f;
        }

        protected override void OnUpdate()
        {
            tickAudioSource.pitch = sourceSpeedMultiplier;
            _shakeGenerator.shakeAmplitude = lifetime * lifetime * visualShakeIncreaseRate * sourceSpeedMultiplier;
            var shake = _shakeGenerator.GetShake();
            shakee.localPosition = shake;
            if (_localGerenadeVisual)
                _localGerenadeVisual.visual.localPosition = shake;

            if (!NetworkServer.active) return;

            if (lifetime > explodeAfter)
            {
                Explode(author);
                return;
            }

            if (_attached)
            {
                rb.linearVelocity = Vector3.zero;

                if (_attached.transform.position != Vector3.zero)
                {
                    var capsule = _attached.motor.Capsule;
                    var yOffset = capsule.center.y * 1.1f * Vector3.up;
                    rb.position = _attached.transform.position + yOffset + _attached.transform.forward * (capsule.radius + collisionRadius);

                    var attachedDelta = _attached.transform.position - _previousAttachedPosition;
                    _collectedAttachedDelta += attachedDelta.magnitude;

                    _previousAttachedPosition = _attached.transform.position;
                }

                if (_collectedAttachedDelta >= detachTotalDelta) Detach();
                else _attached.healthModule.ApplyDamage(new(author, DamageType.Indirect, holdDamage));

                return;
            }

            if (lifetime <= activateGravityAfter) return;
            rb.linearVelocity -= gravityAcceleration * Time.deltaTime * Vector3.up;
        }

        private void Detach()
        {
            TargetDestroyVisual(_attached.netIdentity.connectionToClient);
            _attached.onDeath.RemoveListener(Detach);
            _attached = null;
            RpcPlayDetachAudio();
        }

        [ClientRpc]
        private void RpcPlayDetachAudio()
        {
            detachAudioSource.Play();
        }

        private void Explode(PlayerBase explosionAuthor)
        {
            if (_exploded) return;
            _exploded = true;

            Vector3 pos;
            if (_attached)
            {
                _attached.healthModule.ForceRemoveInvincibility();
                _attached.onDeath.RemoveListener(Detach);
                pos = _attached.transform.position;
            }
            else pos = transform.position;

            var exp = Instantiate(explosion, pos, Quaternion.identity, new InstantiateParameters()
            {
                scene = MapLoader.loadedMap.scene
            });
            exp.GetComponent<DamageSource>().author = explosionAuthor;
            NetworkServer.Spawn(exp);
            DestroyProjectile();
        }

        public override ProjectilePredictionData Predict(float timePassed)
        {
            var timePassedSinceStartedAccelerating = Mathf.Max(timePassed - activateGravityAfter, 0f);

            var flyingVelocity = flySpeed * transform.forward;
            var gravityVelocity = gravityAcceleration * timePassedSinceStartedAccelerating * Vector3.down;
            var velocity = flyingVelocity + gravityVelocity;

            var predictedPos = spawnPosition + flyingVelocity * timePassed + 0.5f * timePassedSinceStartedAccelerating * gravityVelocity;

            return new()
            {
                position = predictedPos,
                rotation = spawnRotation,
                velocity = velocity
            };
        }

        public void Receive(ProjectileCollisionBroadcast broadcast)
        {
            if (_attached) return;
            transform.position = broadcast.point + broadcast.normal * collisionRadius;
        }
    }
}