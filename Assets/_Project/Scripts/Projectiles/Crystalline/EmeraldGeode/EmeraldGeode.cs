using System;
using DG.Tweening;
using Game.Core.Broadcast;
using Game.Core.Damages;
using Game.Core.Damages.Events;
using Game.Core.Projectiles;
using Game.Other;
using Game.Player;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Projectiles.Crystalline.EmeraldGeode
{
    public class EmeraldGeode : NetworkBehaviour, IBroadcastReceiver<SetupDamageSourceBroadcast>
    {
        [Header("Objects")]
        public Transform spawnPointsContainer;
        public Transform visual;
        public DamageTarget damageTarget;
        public EmeraldGeodeProjectile projectile;

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

        private PlayerBase _author;
        private Guid _family;

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
        }

        [Server]
        private void SpawnProjectile(int idx)
        {
            var loopedIdx = idx % _spawnPoints.Length;
            var position = transform.TransformPoint(_spawnPoints[loopedIdx]);
            var rotation = Quaternion.FromToRotation(Vector3.forward, (position - transform.position).normalized);

            Projectile.Spawn(projectile, _author, position, rotation, NetworkTime.time);
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
        }

        private void DestroyGeode()
        {
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

        public void Receive(SetupDamageSourceBroadcast broadcast)
        {
            _author = broadcast.author;
            _family = broadcast.family;
            damageTarget.family = _family;
        }
    }
}