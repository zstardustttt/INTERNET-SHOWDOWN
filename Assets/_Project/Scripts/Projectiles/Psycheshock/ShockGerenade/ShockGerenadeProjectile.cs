using Game.Core.Broadcast;
using Game.Core.Damages;
using Game.Core.Damages.Events;
using Game.Core.Hits;
using Game.Core.Hits.Events;
using Game.Core.Maps;
using Game.Core.Player;
using Game.Core.Projectiles;
using Game.Other;
using Game.Player.Online;
using Mirror;
using UnityEngine;

namespace Game.Projectiles.Psycheshock.ShockGerenade
{
    public class ShockGerenadeProjectile : PredictableProjectile
    {
        [Header("Objects")]
        public GameObject explosionPrefab;
        public GameObject visual;
        public GameObject localGerenadeVisualPrefab;
        public Transform shakee;

        [Space(9)]
        public DamageIdentificationSetup damageIdentificationSetup;
        public HitEntity hitEntity;
        public HitListener attachHitListener;
        public DamageTarget damageTarget;
        public ProjectileCollision collision;

        [Space(9)]
        public AudioSource tickAudioSource;
        public AudioSource attachAudioSource;
        public AudioSource detachAudioSource;
        public AudioSource explosionTriggerAudioSource;
        public ParticleSystem explosionTriggerParticle;

        [Header("Properties")]
        public float speed;
        public float secondarySpeed;
        public float gravityAcceleration;
        public float activateGravityAfter;
        public float explodeAfterPrimary;
        public float explodeAfterSecondary;
        public float explosionDelay;
        public float detachTotalDelta;
        public float collisionRadius;
        public float holdDamage;
        public float visualShakeFrequency;
        public float visualShakeIncreaseRate;

        // client
        private ShockGerenadeLocalVisual _localGerenadeVisual;

        private DamageIdentification _holdDamageIdentification;
        private PlayerCore _attached;
        private Vector3 _previousAttachedPosition;
        private float _collectedAttachedDelta;

        private ShakeGenerator _shakeGenerator;
        private PlayerCore _explosionRequestAuthor;
        private float _explosionTimer;
        private Vector3 _explosionTriggerVelocity;
        private float _holdDamageAmount;

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

        private void OnDestroy()
        {
            if (!_localGerenadeVisual) return;
            Destroy(_localGerenadeVisual.gameObject);
        }

        protected override void OnSpawned()
        {
            _holdDamageIdentification = DamageIdentification.From(damageIdentificationSetup);

            attachHitListener.onHit.AddListener(OnHit);
            damageTarget.onDamage.AddListener(OnDamage);
            collision.onCollision.AddListener(OnCollision);

            PredictSpawn(4, (previousPrediction, prediction) =>
            {
                collision.CheckCollisionBetweenTwoPoints(previousPrediction.position, prediction.position);
            });

            _holdDamageAmount = holdDamage * (explodeAfterPrimary / explodeAfter);
        }

        protected override void OnDestroyed()
        {
            attachHitListener.onHit.RemoveAllListeners();
            collision.onCollision.RemoveAllListeners();

            if (_attached)
            {
                _attached.deathModule.onDeath.RemoveListener(OnAttachedDeath);
                _attached.healthModule.onWishDamage.RemoveListener(OnDamage);
            }
        }

        private void OnHit(HitEvent hitEvent)
        {
            if (!hitEvent.target.transform.root.TryGetComponent(out PlayerCore player)) return;
            if (player == authorReference.author) return;

            Attach(player);
        }

        private void OnDamage(DamageEvent damageEvent)
        {
            var explosionAuthor = damageEvent.damage.type == DamageType.Direct && damageEvent.damage.author
                ? damageEvent.damage.author
                : authorReference.author;

            Explode(explosionAuthor);
        }

        private void OnCollision(Vector3 point, Vector3 normal, Collider other)
        {
            if (_attached) return;
            transform.position = point + normal * collisionRadius;
        }

        private void Attach(PlayerCore player)
        {
            attachHitListener.active = false;
            damageTarget.onDamage.RemoveAllListeners();

            _attached = player;
            _attached.deathModule.onDeath.AddListener(OnAttachedDeath);
            _attached.healthModule.onWishDamage.AddListener(OnDamage);
            _previousAttachedPosition = player.transform.position;

            if (_attached.netIdentity.connectionToClient != null)
                TargetSpawnVisual(_attached.netIdentity.connectionToClient);

            RpcPlayAttachDetachAudio(true);
        }

        private void Detach()
        {
            // Can't be attached afterwards
            damageTarget.onDamage.AddListener(OnDamage);

            _attached.deathModule.onDeath.RemoveListener(OnAttachedDeath);
            _attached.healthModule.onWishDamage.RemoveListener(OnDamage);

            if (_attached.netIdentity.connectionToClient != null)
                TargetDestroyVisual(_attached.netIdentity.connectionToClient);

            RpcPlayAttachDetachAudio(false);

            _attached = null;
        }

        [TargetRpc]
        private void TargetSpawnVisual(NetworkConnectionToClient _)
        {
            var player = OnlinePlayer.localPlayer.player;
            _localGerenadeVisual = Instantiate(localGerenadeVisualPrefab, player.horizontalOrientation).GetComponent<ShockGerenadeLocalVisual>();
            var capsule = player.movementModule.motor.Capsule;
            var yOffset = capsule.center.y * 1.1f * Vector3.up;
            _localGerenadeVisual.transform.localPosition = yOffset + Vector3.forward * (capsule.radius + collisionRadius);
            visual.SetActive(false);

            tickAudioSource.spatialBlend = 0f;
            attachAudioSource.spatialBlend = 0f;
        }

        [TargetRpc]
        private void TargetDestroyVisual(NetworkConnectionToClient _)
        {
            if (!_localGerenadeVisual) return;

            Destroy(_localGerenadeVisual.gameObject);
            visual.SetActive(true);
            tickAudioSource.spatialBlend = 1f;
        }

        private void OnAttachedDeath() => Explode(authorReference.author);

        [ClientRpc]
        private void RpcPlayAttachDetachAudio(bool attach)
        {
            if (attach) attachAudioSource.Play();
            else detachAudioSource.Play();
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

            if (_explosionRequestAuthor)
            {
                _explosionTimer += Time.deltaTime;
                rb.linearVelocity = Vector3.Lerp(_explosionTriggerVelocity, Vector3.zero, _explosionTimer / explosionDelay);

                if (_explosionTimer >= explosionDelay)
                {
                    Vector3 pos;
                    if (_attached)
                    {
                        _attached.deathModule.onDeath.RemoveListener(OnAttachedDeath);
                        _attached.healthModule.onWishDamage.RemoveListener(OnDamage);

                        var damage = new Damage(DamageType.Indirect, 100f, _explosionRequestAuthor, _explosionRequestAuthor.teamReference.team, DamageIdentification.From(damageIdentificationSetup));
                        _attached.healthModule.ApplyDamage(damage);
                        pos = _attached.movementModule.motor.Capsule.bounds.center;
                    }
                    else pos = transform.position;

                    var explosion = MapLoader.NetworkSpawnOnMap(explosionPrefab, pos, Quaternion.identity);
                    explosion.BroadcastOnGameObject(new SetAuthorBroadcast(authorReference.author));
                    explosion.BroadcastOnGameObject(new SetTeamBroadcast(teamReference.team));

                    DestroyProjectile();
                }

                return;
            }

            if (lifetime > explodeAfter)
            {
                Explode(authorReference.author);
                return;
            }

            if (_attached)
            {
                rb.linearVelocity = Vector3.zero;

                if (_attached.transform.position != Vector3.zero)
                {
                    var capsule = _attached.movementModule.motor.Capsule;
                    var yOffset = capsule.center.y * 1.1f * Vector3.up;
                    rb.position = _attached.transform.position + yOffset + _attached.transform.forward * (capsule.radius + collisionRadius);

                    var attachedDelta = _attached.transform.position - _previousAttachedPosition;
                    _collectedAttachedDelta += attachedDelta.magnitude;

                    _previousAttachedPosition = _attached.transform.position;
                }

                if (_collectedAttachedDelta >= detachTotalDelta) Detach();
                else
                {
                    var damage = new Damage(DamageType.Indirect, _holdDamageAmount, authorReference.author, teamReference.team, _holdDamageIdentification);
                    _attached.healthModule.ApplyDamage(damage);
                }

                return;
            }

            if (lifetime <= activateGravityAfter) return;
            rb.linearVelocity -= gravityAcceleration * Time.deltaTime * Vector3.up;
        }

        private void Explode(PlayerCore explosionAuthor)
        {
            if (_explosionRequestAuthor) return;

            hitEntity.active = false;
            _explosionTriggerVelocity = rb.linearVelocity;
            _explosionRequestAuthor = explosionAuthor;
            RpcPlayExplosionTrigger();
        }

        [ClientRpc]
        private void RpcPlayExplosionTrigger()
        {
            explosionTriggerAudioSource.Play();
            explosionTriggerParticle.Play();
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
    }
}