using Game.Core.Damages;
using Game.Core.Damages.Events;
using Game.Core.Hits;
using Game.Core.Hits.Events;
using Game.Core.Items;
using Game.Core.Maps;
using Game.Core.Projectiles;
using Game.Damages;
using Game.Items.Psycheshock;
using Game.Player;
using Mirror;
using UnityEngine;

namespace Game.Projectiles.Psycheshock
{
    public class BOOMerangProjectle : PredictableProjectile
    {
        [Header("Objects")]
        public BasicDamageSource mainDamage;
        public ItemConfig boomerangItem;
        public GameObject explosionPrefab;
        public Transform visualToRotate;

        [Header("Properties")]
        public float maxDistanceLoopDuration;
        public float maxWishPositionDistance;
        public float flySpeed;
        public float directDamage;
        public float explosionDamage;
        public int damageMultiplyCap;
        public int maxReturns;
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

            mainDamage.author = author;
            mainDamage.onHit.AddListener(OnHit);
            mainDamage.onDamage.AddListener(OnDamage);
        }

        public override ProjectilePredictionData Predict(float timePassed)
        {
            if (secondary)
            {
                var velocity = flySpeed * flyDirection;
                var predictedPos = spawnPosition + velocity * timePassed;

                return new()
                {
                    position = predictedPos,
                    rotation = spawnRotation,
                    velocity = velocity,
                };
            }
            else
            {
                var delta = 0.05f;
                var endPos = author ? author.transform.position : transform.position;
                var pos = GetBezierPosition(wishPosition, endPos, timePassed / LoopDuration);
                var prevPos = GetBezierPosition(wishPosition, endPos, (timePassed - delta) / LoopDuration);
                return new()
                {
                    position = pos,
                    rotation = spawnRotation,
                    velocity = (pos - prevPos) / delta,
                };
            }
        }

        protected override void OnCollision(Vector3 point, Vector3 normal, Collider other)
        {
            if (!secondary)
            {
                if (lifetime <= LoopDuration / 2f + 0.05f) return;
                if (lifetime >= LoopDuration && lifetime <= LoopDuration + 0.035f) return;
            }

            Explode(point, normal);
        }

        private void Explode(Vector3 point, Vector3 explosionUp)
        {
            var explosion = Instantiate(explosionPrefab, point, Quaternion.identity, new InstantiateParameters()
            {
                scene = MapLoader.loadedMap.scene
            });
            explosion.transform.up = explosionUp;

            var radialDamage = explosion.GetComponent<RadialDamageSource>();
            radialDamage.author = author;
            radialDamage.outerDamageAmount = explosionDamage * DamageMultiply;
            radialDamage.innerDamageAmount = radialDamage.outerDamageAmount;
            NetworkServer.Spawn(explosion);
            DestroyProjectile();
        }

        private void OnHit(HitEvent hitEvent)
        {
            if (!hitEvent.targetEntity.TryGetComponent(out PlayerBase player)) return;
            if (secondary) return;

            if (player != author || lifetime < LoopDuration / 2f) return;

            author.SetItem(boomerangItem,
                new IntItemArgument("boomerang_damage_multiplier", damageMultiply),
                new IntItemArgument("boomerang_returns", returns + 1)
            );
            DestroyProjectile();
        }

        private void OnDamage(DamageEvent damageEvent)
        {
            if (secondary) return;
            damageMultiply++;
            mainDamage.damageAmount = directDamage * DamageMultiply;
        }

        protected override void OnUpdate()
        {
            visualToRotate.Rotate(transform.up, visualRotationSpeed * Time.deltaTime, Space.World);

            if (!NetworkServer.active) return;
            if (secondary) return;

            if (lifetime >= LoopDuration * 2f)
            {
                Explode(transform.position, transform.up);
                return;
            }

            var t = lifetime / LoopDuration;
            var position = GetBezierPosition(wishPosition, author.verticalOrientation.position, t);

            rb.linearVelocity = (position - _previousBezierPosition) / Time.deltaTime;
            _previousBezierPosition = position;
        }

        private Vector3 GetBezierPosition(Vector3 wishPosition, Vector3 endPosition, float t)
        {
            return Vector3.LerpUnclamped(Vector3.LerpUnclamped(spawnPosition, wishPosition, t * 2f), Vector3.LerpUnclamped(wishPosition, endPosition, t), t);
        }
    }
}