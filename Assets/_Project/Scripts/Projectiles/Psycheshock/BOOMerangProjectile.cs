using Game.Core.Broadcast;
using Game.Core.Damages;
using Game.Core.Damages.Events;
using Game.Core.Hits;
using Game.Core.Hits.Events;
using Game.Core.Items;
using Game.Core.Maps;
using Game.Core.Player;
using Game.Core.Projectiles;
using Game.Damages;
using Mirror;
using UnityEngine;

namespace Game.Projectiles.Psycheshock
{
    public class BOOMerangProjectle : PredictableProjectile
    {
        [Header("Objects")]
        public ProjectileCollision collision;
        public BasicDamageSource mainDamage;
        public HitListener grabHitListener;

        [Space(9)]
        public ItemConfig boomerangItem;
        public GameObject explosionPrefab;
        public GameObject shockEffectPrefab;
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
        [HideInInspector] public int damageMultiply;
        [HideInInspector] public int returns;
        [HideInInspector] public Vector3 wishPosition;
        [HideInInspector] public float wishPositionDistance;

        public float DamageMultiply => Mathf.Min(damageMultiply, damageMultiplyCap);
        public float LoopDuration => maxDistanceLoopDuration * (wishPositionDistance / maxWishPositionDistance);

        private bool _explosionRequested;
        private Vector3 _explosionPoint;
        private Vector3 _explosionNormal;
        private int _explosionFrameCounter;
        private Vector3 _previousBezierPosition;

        protected override void OnSpawned()
        {
            _previousBezierPosition = transform.position;

            collision.onCollision.AddListener(OnCollision);
            mainDamage.onDamage.AddListener(OnDamage);
            grabHitListener.onHit.AddListener(OnHit);

            PredictSpawn(4, (previousPrediction, prediction) =>
            {
                collision.CheckCollisionBetweenTwoPoints(previousPrediction.position, prediction.position);
            });

            mainDamage.damageAmount = directDamage * DamageMultiply;
        }

        protected override void OnDestroyed()
        {
            collision.onCollision.RemoveAllListeners();
            mainDamage.onDamage.RemoveAllListeners();
            grabHitListener.onHit.RemoveAllListeners();
        }

        protected override void OnUpdate()
        {
            visualToRotate.Rotate(transform.up, visualRotationSpeed * Time.deltaTime, Space.World);

            if (!NetworkServer.active) return;
            if (secondary) return;

            if (_explosionRequested)
            {
                if (_explosionFrameCounter == 1)
                {
                    authorReference.author.itemModule.InvokeItemUseEvents(true);
                    Explode(_explosionPoint, _explosionNormal);
                }

                _explosionFrameCounter++;
            }
            else if (lifetime >= LoopDuration * 2f)
            {
                RequestExplosion(transform.position, transform.up);
                return;
            }

            var t = lifetime / LoopDuration;
            var position = GetBezierPosition(wishPosition, authorReference.author.verticalOrientation.position, t);

            rb.linearVelocity = (position - _previousBezierPosition) / Time.deltaTime;
            _previousBezierPosition = position;
        }

        private void OnCollision(Vector3 point, Vector3 normal, Collider other)
        {
            if (!secondary)
            {
                if (lifetime <= LoopDuration / 2f + 0.1f) return;
                if (lifetime >= LoopDuration && lifetime <= LoopDuration + 0.035f) return;
            }

            RequestExplosion(point, normal);
        }

        private void OnDamage(DamageEvent damageEvent)
        {
            var position = damageEvent.target.hitEntity.Collider.bounds.center;
            MapLoader.NetworkSpawnOnMap(shockEffectPrefab, position, Quaternion.identity);

            if (secondary) return;
            damageMultiply++;
            mainDamage.damageAmount = directDamage * DamageMultiply;
        }

        private void OnHit(HitEvent hitEvent)
        {
            if (!hitEvent.target.transform.root.TryGetComponent(out PlayerCore player)) return;
            if (secondary) return;

            if (player != authorReference.author || lifetime < LoopDuration / 2f) return;

            authorReference.author.itemModule.SetItem(boomerangItem,
                new IntItemArgument("boomerang_damage_multiplier", damageMultiply),
                new IntItemArgument("boomerang_returns", returns + 1)
            );
            DestroyProjectile();
        }

        private void RequestExplosion(Vector3 point, Vector3 normal)
        {
            if (secondary)
            {
                Explode(point, normal);
                return;
            }

            _explosionRequested = true;
            _explosionPoint = point;
            _explosionNormal = normal;
        }

        private void Explode(Vector3 point, Vector3 normal)
        {
            var explosion = MapLoader.NetworkSpawnOnMap(explosionPrefab, point, Quaternion.FromToRotation(Vector3.up, normal));
            explosion.BroadcastOnGameObject(new SetAuthorBroadcast(authorReference.author));
            explosion.BroadcastOnGameObject(new SetTeamBroadcast(teamReference.team));

            var radialDamage = explosion.GetComponent<RadialDamageSource>();
            radialDamage.outerDamageAmount = explosionDamage * DamageMultiply;
            radialDamage.innerDamageAmount = radialDamage.outerDamageAmount;

            DestroyProjectile();
        }

        public override ProjectilePredictionData Predict(float timePassed)
        {
            if (secondary)
            {
                var velocity = flySpeed * transform.forward;
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
                var endPos = authorReference.author ? authorReference.author.transform.position : transform.position;
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

        private Vector3 GetBezierPosition(Vector3 wishPosition, Vector3 endPosition, float t)
        {
            return Vector3.LerpUnclamped(Vector3.LerpUnclamped(spawnPosition, wishPosition, t * 2f), Vector3.LerpUnclamped(wishPosition, endPosition, t), t);
        }
    }
}