using System;
using Game.Core.Broadcast;
using Game.Core.Damages;
using Game.Core.Damages.Events;
using Game.Core.Projectiles;
using Game.Player;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Projectiles.Crystalline.EmeraldGeode
{
    public class EmeraldGeode : NetworkBehaviour, IBroadcastReceiver<SetupDamageSourceBroadcast>
    {
        [Header("Objects")]
        public DamageTarget damageTarget;
        public EmeraldGeodeProjectile projectile;

        [Header("Spawning Properties")]
        public float spawnTimeInterval;
        public int initSpawnCount;
        public Vector3 radiusScale;
        public float spawnUpOffset;

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

        public override void OnStartServer()
        {
            SetSpawnCount(initSpawnCount, 0f);
            damageTarget.onDamage.AddListener(OnDamage);
        }

        private void Update()
        {
            if (!NetworkServer.active || _spawnPoints == null) return;

            if (_groupSpawnTimer <= 0f)
            {
                SetSpawnCount(_spawnCount, Random.value * Mathf.PI * 2f);
                _spawnCounter = _spawnCount;
                _groupSpawnTimer = spawnTimeInterval;
            }
            else _groupSpawnTimer -= Time.deltaTime;

            if (_spawnCounter > 0)
            {
                if (_spawnTimer <= 0f)
                {
                    SpawnProjectile(_spawnCounter - 1);
                    _spawnCounter--;
                }
                else _spawnTimer = spawnTimeInterval / _spawnCount;
            }
        }

        private void SpawnProjectile(int idx)
        {
            var position = transform.TransformPoint(_spawnPoints[idx]);
            var rotation = Quaternion.FromToRotation(Vector3.forward, (position - transform.position).normalized);

            Projectile.Spawn(projectile, _author, position + transform.up * spawnUpOffset, rotation, NetworkTime.time);
        }

        private void OnDamage(DamageEvent _)
        {
            SetSpawnCount(_spawnCount - 1, Random.value * Mathf.PI * 2f);
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

                output[i] = new(x * radiusScale.x, y * radiusScale.y, z * radiusScale.z);
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