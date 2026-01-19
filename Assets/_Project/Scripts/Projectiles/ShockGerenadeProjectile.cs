using Game.Core.Maps;
using Game.Core.Projectiles;
using Game.Player;
using Mirror;
using UnityEngine;

namespace Game.Projectiles
{
    public class ShockGerenadeProjectile : PredictableProjectile
    {
        public ProjectileCollision collision;
        public float speed;
        public float gravityAcceleration;
        public float activateGravityAfter;
        public int spawnCheckIterations;
        public float explodeAfter;
        public float detachTotalDelta;
        public DamageDealer explosion;

        private PlayerBase _attached;
        private Vector3 _previousAttachedPosition;
        private bool _attachedToGround;
        private float _collectedAttachedDelta;

        protected override void Init()
        {
            collision.onCollision.AddListener(OnCollision);

            var startCastPoint = _spawnPosition;
            var deltaTime = SpawnDelay / spawnCheckIterations;
            for (int i = 0; i < spawnCheckIterations; i++)
            {
                var prediction = Predict(deltaTime * i);
                collision.CheckCollisionBetweenTwoPoints(startCastPoint, prediction.position);
                _spawnPosition = prediction.position;
            }

            rb.linearVelocity = transform.forward * speed;
        }

        private void OnCollision(Vector3 point, Vector3 normal, Collider other)
        {
            if (_attached || _attachedToGround) return;

            rb.linearVelocity = Vector3.zero;
            var offset = new Vector3
            (
                collision.Bounds.extents.x * normal.x,
                collision.Bounds.extents.y * normal.y,
                collision.Bounds.extents.z * normal.z
            );

            transform.position = point + offset;
            _attachedToGround = true;
        }

        protected override void OnDealerHit(DamageDealer dealer, PlayerBase player, float damage)
        {
            if (_attached) return;
            dealer.active = false;

            _attachedToGround = false;
            _attached = player;
            _attached.onDamage.AddListener(Explode);
            _previousAttachedPosition = player.transform.position;
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
                var attachedDelta = _attached.transform.position - _previousAttachedPosition;
                _collectedAttachedDelta += attachedDelta.magnitude;

                _previousAttachedPosition = _attached.transform.position;
                if (_collectedAttachedDelta >= detachTotalDelta)
                {
                    _attached.onDamage.RemoveListener(Explode);
                    _attached = null;
                }
                else return;
            }

            if (_lifetime <= activateGravityAfter || _attachedToGround || _attached) return;
            rb.linearVelocity -= gravityAcceleration * Time.deltaTime * Vector3.up;
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
            NetworkServer.Destroy(gameObject);
        }

        private void LateUpdate()
        {
            if (!NetworkServer.active) return;

            if (!_attached) return;
            UpdateAttached();
        }

        private void UpdateAttached()
        {
            var forward = _attached.horizontalOrientation.transform.forward;
            var capsule = _attached.motor.Capsule;

            var offsetFromPlayer = forward * capsule.radius;
            var offsetFromProjectile = new Vector3
            (
                collision.Bounds.extents.x * forward.x,
                collision.Bounds.extents.y * forward.y,
                collision.Bounds.extents.z * forward.z
            );

            transform.position = _attached.transform.position + capsule.center + offsetFromPlayer + offsetFromProjectile;
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