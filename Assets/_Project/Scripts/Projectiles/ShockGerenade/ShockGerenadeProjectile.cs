using System;
using System.Linq;
using Game.Core.Events;
using Game.Core.Maps;
using Game.Core.Projectiles;
using Game.Events.HitWatcher;
using Game.Player;
using Mirror;
using UnityEngine;

namespace Game.Projectiles.ShockGerenade
{
    // TODO: Shake visual based on countdown
    // TODO: Explosion effect
    // TODO: Texts on displays
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

        // client
        private GameObject _localGerenadeVisual;

        private PlayerBase _attached;
        private Vector3 _previousAttachedPosition;
        private float _collectedAttachedDelta;

        private Guid _onRegisterHitListenerGuid;

        private void Awake()
        {
            if (!NetworkServer.active) return;
            _onRegisterHitListenerGuid = EventBus<OnRegisterDamage>.Listen((data) =>
            {
                if (data.player != _attached || damageDealers.Contains(data.dealer)) return;
                Explode();
            });
        }

        private void OnDestroy()
        {
            if (NetworkServer.active) EventBus<OnRegisterDamage>.TryCancel(_onRegisterHitListenerGuid);

            if (!_localGerenadeVisual) return;
            Destroy(_localGerenadeVisual);
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
            _previousAttachedPosition = player.transform.position;

            TargetSpawnVisual(_attached.netIdentity.connectionToClient);
        }

        [TargetRpc]
        private void TargetSpawnVisual(NetworkConnectionToClient _)
        {
            var player = NetworkClient.localPlayer.GetComponent<PlayerBase>();
            _localGerenadeVisual = Instantiate(localGerenadeVisualPrefab, player.horizontalOrientation);
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
            Destroy(_localGerenadeVisual);
            visual.SetActive(true);
            tickAudioSource.spatialBlend = 1f;
        }

        protected override void OnUpdate()
        {
            if (!NetworkServer.active) return;

            if (_lifetime > explodeAfter)
            {
                Explode();
                return;
            }

            if (_attached)
            {
                rb.linearVelocity = Vector3.zero;

                if (_attached.transform.position != Vector3.zero)
                {
                    var capsule = _attached.motor.Capsule;
                    transform.position = _attached.transform.position + capsule.center + _attached.transform.forward * (capsule.radius + collisionRadius);

                    var attachedDelta = _attached.transform.position - _previousAttachedPosition;
                    _collectedAttachedDelta += attachedDelta.magnitude;

                    _previousAttachedPosition = _attached.transform.position;
                }

                if (_collectedAttachedDelta >= detachTotalDelta) Detach();
                else
                {
                    if (!_attached.invincible)
                    {
                        _attached.Damage(holdDamage, _owner);
                        if (_owner) _owner.RegisterHit(DamageType.Continuous);
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
            var pos = _attached ? _attached.transform.position : transform.position;

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