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
        public float minWishPositionDistance;
        public float maxWishPositionDistance;
        public BasicDamage damageHitBox;
        public DamageDealer grabHitBox;
        public ItemConfig boomerangItem;
        public float directDamage;
        public float explosionDamage;
        public int damageMultiplyCap;
        public RadialDamage explosionPrefab;

        [HideInInspector] public float wishPositionDistance;
        [HideInInspector] public int damageMultiply;
        [HideInInspector] public Vector3 wishPosition;

        public float DamageMultiply => Mathf.Min(damageMultiply, damageMultiplyCap);
        public float LoopDuration => maxDistanceLoopDuration * (wishPositionDistance / maxWishPositionDistance);

        private void Start()
        {
            if (!NetworkServer.active) return;
            previousBezierPosition = transform.position;
        }

        public override ProjectilePredictionData Predict(float timePassed)
        {
            return new()
            {
                position = GetBezierPosition(wishPosition, _owner.transform.position, timePassed / LoopDuration),
                rotation = _spawnRotation,
                velocity = Vector3.zero,
            };
        }

        protected override void OnCollision(Vector3 point, Vector3 normal, Collider other)
        {
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

            if (dealer == damageHitBox)
            {
                if (player == _owner) return;
                damageMultiply++;
                damageHitBox.baseDamage = directDamage * DamageMultiply;
            }
            else if (dealer == grabHitBox)
            {
                if (player != _owner || _lifetime < LoopDuration / 2f) return;
                _owner.SetItem(boomerangItem, new BOOMerangDamageMultiplier() { damageMultiplier = damageMultiply });
                DestroyProjectile();
            }
        }

        private Vector3 previousBezierPosition;

        protected override void OnUpdate()
        {
            if (!NetworkServer.active) return;

            var t = _lifetime / LoopDuration;
            var position = GetBezierPosition(wishPosition, _owner.verticalOrientation.position, t);

            rb.linearVelocity = (position - previousBezierPosition) / Time.deltaTime;
            previousBezierPosition = position;
        }

        private Vector3 GetBezierPosition(Vector3 wishPosition, Vector3 endPosition, float t)
        {
            return Vector3.LerpUnclamped(Vector3.LerpUnclamped(_spawnPosition, wishPosition, t * 2f), Vector3.LerpUnclamped(wishPosition, endPosition, t), t);
        }
    }
}