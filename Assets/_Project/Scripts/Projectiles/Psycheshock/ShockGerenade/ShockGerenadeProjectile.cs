using System;
using Game.Core.Damage;
using Game.Core.Maps;
using Game.Core.Projectiles;
using Game.Other;
using Game.Player;
using Mirror;
using UnityEngine;

namespace Game.Projectiles.Psycheshock.ShockGerenade
{
    public class ShockGerenadeProjectile : PredictableProjectile
    {
        public float speed;
        public float secondarySpeed;
        public float gravityAcceleration;
        public float activateGravityAfter;
        public float explodeAfterPrimary;
        public float explodeAfterSecondary;
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
        public DamageReceiver damageReceiver;

        public AudioSource attachAudioSource;
        public AudioSource detachAudioSource;

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
            damageReceiver.onDamage.AddListener(OnDamageReceived);
            damageReceiver.Register(Guid.NewGuid());
        }

        private void OnDamageReceived(DamageDealer dealer, float _)
        {
            if (dealer.damageType == DamageType.None || _attached) return;
            if (dealer.damageType == DamageType.Direct && dealer.owner) _owner = dealer.owner;
            Explode();
        }

        private void OnDestroy()
        {
            if (NetworkServer.active)
            {
                damageReceiver.Unregister();
                damageReceiver.onDamage.RemoveListener(OnDamageReceived);
            }

            if (!_localGerenadeVisual) return;
            Destroy(_localGerenadeVisual.gameObject);
        }

        protected override void OnCollision(Vector3 point, Vector3 normal, Collider other)
        {
            if (_attached) return;

            transform.position = point + normal * collisionRadius;
        }

        protected override void OnDealerHit(DamageDealer dealer, DamageReceiver receiver, float damage)
        {
            if (_attached) return;
            if (!receiver.TryGetComponent(out PlayerBase player)) return;
            if (player == _owner) return;
            dealer.active = false;

            _attached = player;
            _attached.onReceiveDamage.AddListener(OnAttachedReceiveDamage);
            _attached.onDeath.AddListener(Detach);
            _previousAttachedPosition = player.transform.position;

            TargetSpawnVisual(_attached.netIdentity.connectionToClient);
            RpcPlayAttachAudio();
        }

        private void OnAttachedReceiveDamage(DamageDealer dealer)
        {
            if (dealer.damageType == DamageType.Direct && dealer.owner) _owner = dealer.owner;
            Explode();
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
            _shakeGenerator.shakeAmplitude = _lifetime * _lifetime * visualShakeIncreaseRate * sourceSpeedMultiplier;
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
        }

        private void MainUpdate()
        {
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
            }

            if (_lifetime <= activateGravityAfter || _attached) return;
            rb.linearVelocity -= gravityAcceleration * Time.deltaTime * Vector3.up;
        }

        private void Detach()
        {
            TargetDestroyVisual(_attached.netIdentity.connectionToClient);
            _attached.onDeath.RemoveListener(Detach);
            _attached.onReceiveDamage.RemoveListener(OnAttachedReceiveDamage);
            _attached = null;
            RpcPlayDetachAudio();
        }

        [ClientRpc]
        private void RpcPlayDetachAudio()
        {
            detachAudioSource.Play();
        }

        private void Explode()
        {
            if (_exploded) return;
            _exploded = true;

            Vector3 pos;
            if (_attached)
            {
                _attached.ForceRemoveInvincibility();
                _attached.onDeath.RemoveListener(Detach);
                _attached.onReceiveDamage.RemoveListener(OnAttachedReceiveDamage);
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

            var flyingVelocity = flySpeed * transform.forward;
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