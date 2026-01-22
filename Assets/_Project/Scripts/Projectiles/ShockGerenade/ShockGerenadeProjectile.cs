using System;
using Game.Core.Events;
using Game.Core.Maps;
using Game.Core.Projectiles;
using Game.Events.HitWatcher;
using Game.Other;
using Game.Player;
using Mirror;
using UnityEngine;

namespace Game.Projectiles.ShockGerenade
{
    // TODO: Explosion effect
    public class ShockGerenadeProjectile : PredictableProjectile
    {
        public float speed;
        public float gravityAcceleration;
        public float activateGravityAfter;
        public float explodeAfter;
        public float detachTotalDelta;
        public DamageDealer explosion;
        public float collisionRadius;
        public GameObject visual;
        public GameObject localGerenadeVisualPrefab;
        public AudioSource tickAudioSource;
        public float holdDamage;
        public Transform shakee;
        public float visualShakeFrequency;
        public float visualShakeIncreaseRate;

        // client
        private ShockGerenadeLocalVisual _localGerenadeVisual;

        private PlayerBase _attached;
        private Vector3 _previousAttachedPosition;
        private float _collectedAttachedDelta;

        private Guid _onRegisterDamageListenerGuid;
        private float _damageInterval;
        private float _damageTimer;

        private ShakeGenerator _shakeGenerator;

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
            _onRegisterDamageListenerGuid = EventBus<OnRegisterDamage>.Listen((data) =>
            {
                if (data.player != _attached) return;
                Explode();
            });
        }

        private void OnDestroy()
        {
            if (NetworkServer.active) EventBus<OnRegisterDamage>.TryCancel(_onRegisterDamageListenerGuid);

            if (!_localGerenadeVisual) return;
            Destroy(_localGerenadeVisual.gameObject);
        }

        protected override void OnCollision(Vector3 point, Vector3 normal, Collider other)
        {
            if (_attached) return;

            transform.position = point + normal * collisionRadius;
        }

        protected override void OnDealerHit(DamageDealer dealer, PlayerBase player, float damage)
        {
            if (_attached) return;
            dealer.active = false;

            _attached = player;
            _attached.onDeath.AddListener(Detach);
            _damageInterval = _attached.config.damageInvincibilityDuration;
            _previousAttachedPosition = player.transform.position;

            TargetSpawnVisual(_attached.netIdentity.connectionToClient);
        }

        [TargetRpc]
        private void TargetSpawnVisual(NetworkConnectionToClient _)
        {
            var player = NetworkClient.localPlayer.GetComponent<PlayerBase>();
            _localGerenadeVisual = Instantiate(localGerenadeVisualPrefab, player.horizontalOrientation).GetComponent<ShockGerenadeLocalVisual>();
            _localGerenadeVisual.transform.localPosition = player.motor.Capsule.center + Vector3.forward * (player.motor.Capsule.radius + collisionRadius);
            visual.SetActive(false);
            tickAudioSource.spatialBlend = 0f;
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
            _shakeGenerator.shakeAmplitude = _lifetime * _lifetime * visualShakeIncreaseRate;
            var shake = _shakeGenerator.GetShake();
            shakee.localPosition = shake;
            if (_localGerenadeVisual)
                _localGerenadeVisual.visual.localPosition = shake;

            if (!NetworkServer.active) return;

            if (_lifetime > explodeAfter)
            {
                Explode();
                return;
            }

            MainUpdate();
            if (_damageTimer > 0f) _damageTimer -= Time.deltaTime;
        }

        private void MainUpdate()
        {
            if (_attached)
            {
                rb.linearVelocity = Vector3.zero;

                if (_attached.transform.position != Vector3.zero)
                {
                    var capsule = _attached.motor.Capsule;
                    rb.position = _attached.transform.position + capsule.center + _attached.transform.forward * (capsule.radius + collisionRadius);

                    var attachedDelta = _attached.transform.position - _previousAttachedPosition;
                    _collectedAttachedDelta += attachedDelta.magnitude;

                    _previousAttachedPosition = _attached.transform.position;
                }

                if (_collectedAttachedDelta >= detachTotalDelta) Detach();
                else
                {
                    if (_damageTimer <= 0f)
                    {
                        _attached.Damage(holdDamage, _owner, false);
                        if (_owner) _owner.RegisterHit(DamageType.Continuous);
                        _damageTimer = _damageInterval;
                    }

                    return;
                }
            }

            if (_lifetime <= activateGravityAfter || _attached) return;
            rb.linearVelocity -= gravityAcceleration * Time.deltaTime * Vector3.up;
        }

        private void Detach()
        {
            TargetDestroyVisual(_attached.netIdentity.connectionToClient);
            _attached.onDeath.RemoveListener(Detach);
            _attached = null;
        }

        private void Explode()
        {
            Vector3 pos;
            if (_attached)
            {
                _attached.ForceRemoveInvincibility();
                _attached.onDeath.RemoveListener(Detach);
                pos = _attached.transform.position;
            }
            else pos = transform.position;

            var exp = Instantiate(explosion.gameObject, pos, Quaternion.identity, new InstantiateParameters()
            {
                scene = MapLoader.loadedMap.scene
            });
            exp.GetComponent<DamageDealer>().owner = _owner;
            NetworkServer.Spawn(exp);
            DestroyProjectile();
        }

        public override ProjectilePredictionData Predict(float timePassed)
        {
            var timePassedSinceStartedAccelerating = Mathf.Max(timePassed - activateGravityAfter, 0f);

            var flyingVelocity = speed * transform.forward;
            var gravityVelocity = gravityAcceleration * timePassedSinceStartedAccelerating * Vector3.down;
            var velocity = flyingVelocity + gravityVelocity;

            var predictedPos = _spawnPosition + flyingVelocity * timePassed + 0.5f * timePassedSinceStartedAccelerating * gravityVelocity;

            return new()
            {
                position = predictedPos,
                rotation = _spawnRotation,
                velocity = velocity
            };
        }
    }
}