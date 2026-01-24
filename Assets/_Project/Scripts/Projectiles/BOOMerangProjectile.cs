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
        public float loopDuration;
        public float minWishPositionDistance;
        public float maxWishPositionDistance;
        public BasicDamage damageHitBox;
        public DamageDealer grabHitBox;
        public ItemConfig boomerangItem;
        public float directDamage;
        public float explosionDamage;
        public int damageMultiplyCap;
        public RadialDamage explosionPrefab;

        [HideInInspector] public int damageMultiply;
        [HideInInspector] public Vector3 wishPosition;
        [HideInInspector] public Vector3 endPosition;

        public float DamageMultiply => Mathf.Min(damageMultiply, damageMultiplyCap);

        public override ProjectilePredictionData Predict(float timePassed)
        {
            var t = timePassed / loopDuration;
            var position = Vector3.Lerp(Vector3.Lerp(_spawnPosition, wishPosition, t * 2f), Vector3.Lerp(wishPosition, endPosition, t), t);
            var velocity = 2f * (1f - t) * (wishPosition - _spawnPosition) + t * (endPosition - wishPosition);
            return new()
            {
                position = position,
                rotation = _spawnRotation,
                velocity = velocity,
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
                if (player != _owner || _lifetime < loopDuration / 2f) return;
                _owner.SetItem(boomerangItem, new BOOMerangDamageMultiplier() { damageMultiplier = damageMultiply });
                DestroyProjectile();
            }
        }

        protected override void OnUpdate()
        {
            if (!NetworkServer.active) return;

            var t = _lifetime / loopDuration;
            rb.linearVelocity = 2f * (1f - t) * (wishPosition - _spawnPosition) + t * (endPosition - wishPosition);
        }
    }
}