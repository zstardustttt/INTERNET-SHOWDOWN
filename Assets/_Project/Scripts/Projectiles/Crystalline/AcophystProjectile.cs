using Game.Core.Broadcast;
using Game.Core.Damages;
using Game.Core.Damages.Events;
using Game.Core.Maps;
using Game.Core.Projectiles;
using Mirror;
using UnityEngine;

namespace Game.Projectiles.Crystalline
{
    public class AcophystProjectile : PredictableProjectile
    {
        [Header("Objects")]
        public ProjectileCollision collision;
        public DamageSource mainDamage;
        public Transform visual;
        public Transform subVisual;
        public Transform model;

        [Space(9)]
        public AcophystProjectile projectileToSpawnOnHit;
        public GameObject breakEffectPrefab;

        [Header("Properties")]
        public float destroyDelay;
        public float visualRotationSpeed;
        public float resolveRadius;
        public float enableDamageAfter;
        public float flySpeed;
        public float beginMaxDot;
        public float continueMaxDot;
        public float beginMaxDotDuration;
        public float desroyOnBounceAfter;
        public float maxLifetime;
        public float gravityAcceleration;
        public float activateGravityAfter;
        public float autoAimOnClosestFactor;

        private Vector3 _direction;
        private Vector3 _previousNormal;
        private Vector3 _previousPoint;
        private float _gravityTimer;
        [SyncVar] private Vector3 _wishVelocity;

        private bool _broken;
        private float _breakTimer;

        protected override void OnSpawned()
        {
            _direction = transform.forward;

            collision.onCollision.AddListener(OnCollision);
            mainDamage.onDamage.AddListener(OnDamage);

            if (enableDamageAfter != 0f) mainDamage.active = false;

            PredictSpawn(1, (previous, current) =>
            {
                collision.CheckCollisionBetweenTwoPoints(previous.position, current.position);
            });
            _wishVelocity = rb.linearVelocity;
        }

        protected override void OnDestroyed()
        {
            collision.onCollision.RemoveAllListeners();
            mainDamage.onDamage.RemoveAllListeners();
        }

        protected override void OnUpdate()
        {
            if (_broken)
            {
                _breakTimer += Time.deltaTime;
                if (NetworkServer.active && _breakTimer >= destroyDelay)
                {
                    DestroyProjectile();
                }

                return;
            }

            visual.up = _wishVelocity.normalized;
            subVisual.Rotate(Vector3.up, visualRotationSpeed * Time.deltaTime, Space.Self);
            if (!NetworkServer.active) return;

            _wishVelocity = _direction * flySpeed - _gravityTimer * gravityAcceleration * Vector3.up;
            rb.linearVelocity = _wishVelocity;

            if (lifetime > activateGravityAfter) _gravityTimer += Time.deltaTime;
            if (lifetime > maxLifetime)
            {
                Break(false, transform.position, Vector3.up);
                return;
            }

            if (enableDamageAfter != 0f && lifetime > enableDamageAfter && !mainDamage.active) mainDamage.active = true;
        }

        private void OnCollision(Vector3 point, Vector3 normal, Collider other)
        {
            if (lifetime > desroyOnBounceAfter) Break(false, point, normal);
            else
            {
                Bounce(point, normal);
                _previousNormal = normal;
                _previousPoint = point;
            }
        }

        private void OnDamage(DamageEvent _)
        {
            if (!projectileToSpawnOnHit) return;

            var axisEuler = transform.up * 90f;
            var rotation1 = Quaternion.Euler(axisEuler);
            var rotation2 = Quaternion.Euler(-axisEuler);

            Spawn(projectileToSpawnOnHit, author, transform.position, rotation1, NetworkTime.time);
            Spawn(projectileToSpawnOnHit, author, transform.position, rotation2, NetworkTime.time);

            Break(true, Vector3.zero, Vector3.up);
        }

        private void Bounce(Vector3 point, Vector3 normal)
        {
            var movementDirection = _wishVelocity.normalized;

            var maxDot = lifetime <= beginMaxDotDuration ? beginMaxDot : continueMaxDot;
            if (Vector3.Dot(normal, movementDirection) > maxDot) return;

            if (normal == _previousNormal && Vector3.Distance(point, _previousPoint) < 0.1f) return;

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

            _direction = (Vector3.Reflect(movementDirection, normal) + directionToClosest * autoAimOnClosestFactor).normalized;
            _wishVelocity = _direction * flySpeed;
            transform.forward = _direction;
            transform.position = point + normal * resolveRadius;
            _gravityTimer = 0f;
        }

        [Server]
        private void Break(bool split, Vector3 effectPoint, Vector3 effectUp)
        {
            if (_broken) return;

            rb.linearVelocity = Vector3.zero;
            mainDamage.active = false;
            collision.active = false;
            collision.Collider.isTrigger = true;
            _broken = true;


            if (split)
            {
                // TODO: split effect
            }
            else
            {
                var breakEffect = MapLoader.NetworkSpawnOnMap(breakEffectPrefab, effectPoint, Quaternion.FromToRotation(Vector3.up, effectUp));
                breakEffect.BroadcastOnChildren(new SetupDamageSourceBroadcast()
                {
                    author = author,
                    family = author.healthModule.family,
                });
            }

            RpcBreak();
        }

        [ClientRpc]
        private void RpcBreak()
        {
            _broken = true;
            model.gameObject.SetActive(false);
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
                velocity = velocity,
                rotation = spawnRotation
            };
        }
    }
}