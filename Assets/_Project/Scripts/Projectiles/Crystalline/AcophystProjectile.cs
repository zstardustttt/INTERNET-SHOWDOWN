using Game.Core.Damage;
using Game.Core.Maps;
using Game.Core.Projectiles;
using Mirror;
using UnityEngine;

namespace Game.Projectiles.Crystalline
{
    public class AcophystProjectile : PredictableProjectile
    {
        public float flySpeed;
        public float beginMaxDot;
        public float continueMaxDot;
        public float beginMaxDotDuration;
        public float maxLifetime;
        public float gravityAcceleration;
        public float activateGravityAfter;
        public float autoAimOnClosestFactor;
        public AcophystProjectile projectileToSpawnOnHit;

        private Vector3 _direction;
        private Vector3 _previousNormal;
        private Vector3 _previousPoint;
        private float _gravityTimer;

        private void Start()
        {
            _direction = transform.forward;
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
                velocity = velocity,
                rotation = _spawnRotation
            };
        }

        protected override void OnCollision(Vector3 point, Vector3 normal, Collider other)
        {
            if (_lifetime > maxLifetime) DestroyProjectile();
            else
            {
                Bounce(point, normal);
                _previousNormal = normal;
                _previousPoint = point;
            }
        }

        private void Bounce(Vector3 point, Vector3 normal)
        {
            var maxDot = _lifetime <= beginMaxDotDuration ? beginMaxDot : continueMaxDot;
            if (Vector3.Dot(normal, _direction) > maxDot) return;

            if (normal == _previousNormal && Vector3.Distance(point, _previousPoint) < 0.1f) return;

            var directionToClosest = Vector3.zero;
            var closestDistance = 2000f;
            foreach (var (_, player) in MapLoader.loadedMap.players)
            {
                if (player.dead || player == _owner) continue;
                var distance = Vector3.Distance(player.transform.position, transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    directionToClosest = (player.transform.position - transform.position).normalized;
                }
            }

            _direction = (Vector3.Reflect(_direction, normal) + directionToClosest * autoAimOnClosestFactor).normalized;
            transform.forward = _direction;
            _gravityTimer = 0f;
        }

        protected override void OnDealerHit(DamageDealer dealer, DamageReceiver receiver, float damage)
        {
            if (!projectileToSpawnOnHit || receiver == _owner.damageReceiver) return;

            var axisEuler = transform.up * 90f;
            var rotation1 = Quaternion.Euler(axisEuler);
            var rotation2 = Quaternion.Euler(-axisEuler);

            var proj1 = Spawn(projectileToSpawnOnHit, _owner, transform.position, transform.position, rotation1);
            proj1.SetupPrediction(NetworkTime.time, 0);

            var proj2 = Spawn(projectileToSpawnOnHit, _owner, transform.position, transform.position, rotation2);
            proj2.SetupPrediction(NetworkTime.time, 0);
        }

        protected override void OnUpdate()
        {
            if (!NetworkServer.active) return;

            rb.linearVelocity = _direction * flySpeed - _gravityTimer * gravityAcceleration * Vector3.up;
            if (_lifetime > activateGravityAfter) _gravityTimer += Time.deltaTime;
        }
    }
}