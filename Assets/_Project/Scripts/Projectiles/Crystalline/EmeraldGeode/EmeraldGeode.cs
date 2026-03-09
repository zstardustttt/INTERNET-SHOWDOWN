using System;
using DG.Tweening;
using Game.Core.Damages;
using Game.Core.Damages.Events;
using Game.Core.Hits;
using Game.Core.Maps;
using Game.Core.Projectiles;
using Game.Other;
using Game.Player;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Projectiles.Crystalline.EmeraldGeode
{
    public class EmeraldGeode : NetworkBehaviour
    {
        [Header("Objects")]
        public Transform spawnPointsContainer;
        public Transform visual;
        public HitEntity hitEntity;
        public DamageTarget damageTarget;
        public AuthorReference authorReference;
        public TeamReference teamReference;
        public EmeraldGeodeProjectile projectile;
        public ParticleSystem[] spawnEffects;
        public GameObject breakEffect;

        [Header("Properties")]
        public float maxLifetime;

        [Header("Spawning Properties")]
        public float groupSpawnTimeInterval;
        public float spawnTimeInterval;
        public int initSpawnCount;
        public Vector3 radiusScale;
        public float spawnUpOffset;

        [Header("Damage Shake")]
        public float damageShakeAmplitude;
        public float damageShakeFrequency;
        public float damageShakeFalloffSpeed;

        private ShakeGenerator _shakeGenerator;

        private Vector3[] _spawnPoints;
        private int _spawnCount;
        private int _spawnCounter;
        private float _groupSpawnTimer;
        private float _spawnTimer;

        private float _lifetime;

        protected override void OnValidate()
        {
            base.OnValidate();
            SetSpawnCount(initSpawnCount, 0f);
        }

        private void Awake()
        {
            _shakeGenerator = new();
        }

        public override void OnStartServer()
        {
            SetSpawnCount(initSpawnCount, 0f);
            damageTarget.onDamage.AddListener(OnDamage);
            hitEntity.family = Guid.NewGuid();
        }

        private void OnDestroy()
        {
            foreach (Transform spawnPoint in spawnPointsContainer)
            {
                spawnPoint.transform.DOKill();
            }
        }

        private void Update()
        {
            visual.localPosition = _shakeGenerator.GetShake();
            if (!NetworkServer.active || _spawnPoints == null) return;

            if (_spawnCounter > 0)
            {
                if (_spawnTimer <= 0f)
                {
                    _spawnCounter--;
                    SpawnProjectile(_spawnCounter);

                    _spawnTimer = spawnTimeInterval;
                }
                else _spawnTimer -= Time.deltaTime;
            }

            if (_groupSpawnTimer <= 0f)
            {
                SetSpawnCount(_spawnCount, Random.value * Mathf.PI * 2f);
                _spawnCounter = _spawnCount;
                _groupSpawnTimer = groupSpawnTimeInterval;
                _spawnTimer = spawnTimeInterval;
            }
            else _groupSpawnTimer -= Time.deltaTime;

            if (_lifetime >= maxLifetime) DestroyGeode();
            else _lifetime += Time.deltaTime;
        }

        [Server]
        private void SpawnProjectile(int idx)
        {
            var loopedIdx = idx % _spawnPoints.Length;
            var position = transform.TransformPoint(_spawnPoints[loopedIdx]);
            var rotation = Quaternion.FromToRotation(Vector3.forward, (position - transform.position).normalized);

            var proj = Projectile.Spawn(projectile, authorReference.author, teamReference.team, position, rotation, NetworkTime.time, (proj) =>
            {
                proj.hitEntity.family = hitEntity.family;
            });

            RpcOnProjectileSpawn(idx, position);
        }

        private void OnDamage(DamageEvent _)
        {
            SetSpawnCount(_spawnCount - 1, Random.value * Mathf.PI * 2f);
            RpcOnDamage();
        }

        [ClientRpc]
        private void RpcOnDamage()
        {
            _shakeGenerator.Shake(damageShakeAmplitude, damageShakeFrequency, damageShakeFalloffSpeed);
        }

        private void SetSpawnCount(int count, float spin)
        {
            if (count <= 0)
            {
                DestroyGeode();
                return;
            }

            _spawnCount = count;
            _spawnPoints = UniformDistribute(count, spin);
            if (NetworkServer.active) RpcUpdateSpawnPointVisuals(_spawnPoints);
        }

        [ClientRpc]
        private void RpcUpdateSpawnPointVisuals(Vector3[] spawnPoints)
        {
            var idx = 0;
            foreach (Transform spawnPoint in spawnPointsContainer)
            {
                if (idx < spawnPoints.Length)
                {
                    spawnPoint.gameObject.SetActive(true);
                    spawnPoint.transform.localPosition = spawnPoints[idx];

                    spawnPoint.transform.DOKill();
                    spawnPoint.transform.localScale = Vector3.zero;
                    spawnPoint.transform.DOScale(Vector3.one * Random.Range(0.75f, 1.2f), 0.045f * idx);
                }
                else spawnPoint.gameObject.SetActive(false);

                idx++;
            }
        }

        [ClientRpc]
        private void RpcOnProjectileSpawn(int idx, Vector3 position)
        {
            var spawnPoint = spawnPointsContainer.GetChild(idx);
            spawnPoint.DOKill();
            spawnPoint.gameObject.SetActive(false);

            var spawnEffect = spawnEffects[idx];
            spawnEffect.transform.position = position;
            spawnEffect.Play(true);
        }

        private void DestroyGeode()
        {
            MapLoader.NetworkSpawnOnMap(breakEffect, transform.position, transform.rotation);
            damageTarget.onDamage.RemoveAllListeners();
            NetworkServer.Destroy(gameObject);
        }

        private Vector3[] UniformDistribute(int count, float spin)
        {
            var output = new Vector3[count];
            var dlong = Mathf.PI * (3f - Mathf.Sqrt(5f));

            for (int i = 0; i < count; i++)
            {
                var y = i / (float)count;
                var radius = Mathf.Sqrt(1f - y * y);

                var goldenRatioI = dlong * i;
                var x = Mathf.Cos(spin + goldenRatioI) * radius;
                var z = Mathf.Sin(spin + goldenRatioI) * radius;

                output[i] = new(x * radiusScale.x, y * radiusScale.y + spawnUpOffset, z * radiusScale.z);
            }

            return output;
        }

        private void OnDrawGizmosSelected()
        {
            if (_spawnPoints == null) return;

            Gizmos.color = Color.red;
            for (int i = 0; i < _spawnCount; i++)
            {
                Gizmos.DrawSphere(transform.TransformPoint(_spawnPoints[i]), 0.05f);
            }
        }
    }
}