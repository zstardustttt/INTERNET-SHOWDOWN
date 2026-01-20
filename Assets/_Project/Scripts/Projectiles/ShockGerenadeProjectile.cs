using Game.Core.Maps;
using Game.Core.Projectiles;
using Game.Player;
using Mirror;
using UnityEngine;

namespace Game.Projectiles
{
    public class ShockGerenadeProjectile : PredictableProjectile
    {
        public float speed;
        public float gravityAcceleration;
        public float activateGravityAfter;
        public float explodeAfter;
        public float detachTotalDelta;
        public DamageDealer explosion;
        public float collisionRadius;

        private PlayerBase _attached;
        private Vector3 _previousAttachedPosition;
        private float _collectedAttachedDelta;

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

            if (_lifetime <= activateGravityAfter || _attached) return;
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
            DestroyProjectile();
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
            var offsetFromProjectile = forward * collisionRadius;

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