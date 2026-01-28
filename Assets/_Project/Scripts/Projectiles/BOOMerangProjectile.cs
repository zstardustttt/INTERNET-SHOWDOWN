using Game.Core.Damage;
using Game.Core.Items;
using Game.Core.Maps;
using Game.Core.Projectiles;
using Game.Damage;
using Game.Items;
using Game.Player;
using Mirror;
using UnityEngine;

namespace Game.Projectiles
{
    public class BOOMerangProjectle : PredictableProjectile
    {
        public float maxDistanceLoopDuration;
        public float maxWishPositionDistance;
        public float flySpeed;
        public BasicDamage damageHitBox;
        public DamageDealer grabHitBox;
        public ItemConfig boomerangItem;
        public float directDamage;
        public float explosionDamage;
        public int damageMultiplyCap;
        public int maxReturns;
        public RadialDamage explosionPrefab;
        public Transform visualToRotate;
        public float visualRotationSpeed;

        [HideInInspector] public bool secondary;
        [HideInInspector] public Vector3 flyDirection;
        [HideInInspector] public float wishPositionDistance;
        [HideInInspector] public int damageMultiply;
        [HideInInspector] public int returns;
        [HideInInspector] public Vector3 wishPosition;

        public float DamageMultiply => Mathf.Min(damageMultiply, damageMultiplyCap);
        public float LoopDuration => maxDistanceLoopDuration * (wishPositionDistance / maxWishPositionDistance);

        private Vector3 _previousBezierPosition;

        private void Start()
        {
            if (!NetworkServer.active) return;
            _previousBezierPosition = transform.position;
        }

        public override ProjectilePredictionData Predict(float timePassed)
        {
            if (secondary)
            {
                var velocity = flySpeed * flyDirection;
                var predictedPos = _spawnPosition + velocity * timePassed;

                return new()
                {
                    position = predictedPos,
                    rotation = _spawnRotation,
                    velocity = velocity,
                };
            }
            else
            {
                var delta = 0.05f;
                var endPos = _owner ? _owner.transform.position : transform.position;
                var pos = GetBezierPosition(wishPosition, endPos, timePassed / LoopDuration);
                var prevPos = GetBezierPosition(wishPosition, endPos, (timePassed - delta) / LoopDuration);
                return new()
                {
                    position = pos,
                    rotation = _spawnRotation,
                    velocity = (pos - prevPos) / delta,
                };
            }
        }

        protected override void OnCollision(Vector3 point, Vector3 normal, Collider other)
        {
            if (!secondary)
            {
                if (_lifetime <= LoopDuration / 2f + 0.025f) return;
                if (_lifetime >= LoopDuration && _lifetime <= LoopDuration + 0.035f) return;
            }

            var explosion = Instantiate(explosionPrefab.gameObject, point, Quaternion.identity, new InstantiateParameters()
            {
                scene = MapLoader.loadedMap.scene
            });
            var radialDamage = explosion.GetComponent<RadialDamage>();
            radialDamage.owner = _owner;
            radialDamage.outerDamage = explosionDamage * DamageMultiply;
            radialDamage.innerDamage = radialDamage.outerDamage;
            NetworkServer.Spawn(explosion);
            DestroyProjectile();
        }

        protected override void OnDealerHit(DamageDealer dealer, DamageReceiver receiver, float damage)
        {
            if (!receiver.TryGetComponent(out PlayerBase player)) return;
            if (secondary) return;

            if (dealer == damageHitBox)
            {
                if (player == _owner) return;
                damageMultiply++;
                damageHitBox.baseDamage = directDamage * DamageMultiply;
            }
            else if (dealer == grabHitBox)
            {
                if (player != _owner || _lifetime < LoopDuration / 2f) return;
                _owner.SetItem(boomerangItem, new BOOMerangDamageMultiplier() { damageMultiplier = damageMultiply, returns = returns + 1 });
                DestroyProjectile();
            }
        }

        protected override void OnUpdate()
        {
            visualToRotate.Rotate(transform.up, visualRotationSpeed * Time.deltaTime, Space.World);

            if (!NetworkServer.active) return;
            if (secondary) return;

            var t = _lifetime / LoopDuration;
            var position = GetBezierPosition(wishPosition, _owner.verticalOrientation.position, t);

            rb.linearVelocity = (position - _previousBezierPosition) / Time.deltaTime;
            _previousBezierPosition = position;
        }

        private Vector3 GetBezierPosition(Vector3 wishPosition, Vector3 endPosition, float t)
        {
            return Vector3.LerpUnclamped(Vector3.LerpUnclamped(_spawnPosition, wishPosition, t * 2f), Vector3.LerpUnclamped(wishPosition, endPosition, t), t);
        }
    }
}