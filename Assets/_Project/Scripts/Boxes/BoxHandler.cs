using System.Collections.Generic;
using Game.Boxes.Events;
using Game.Core.Events;
using Game.Core.Hits;
using Game.Core.Hits.Events;
using Game.Core.Maps;
using Game.GameLoop;
using Game.GameLoop.Events;
using Game.Player;
using Game.Player.Events;
using Mirror;
using UnityEngine;

namespace Game.Boxes
{
    // This object is only active on the server
    public class BoxHandler : MonoBehaviour
    {
        public GameObject boxPrefab;
        public HitLayer boxesLayer;

        [Header("Ambient Spawning")]
        public int maxAmbientSpawnFails;
        public float ambientSpawnRate;
        public int maxBoxesPerPlayer;

        [Header("Need Spawning")]
        public int maxNeedSpawnFails;
        public float offsetTowardsPlayerFactor;

        [Header("Box spawning settings")]
        public float maxGroundAngle;
        public float spawnMargin;
        public float spawnYOffset;

        private bool _active;
        private float _timer;
        private int _spawnedBoxesCounter;
        private LayerMask _enviromentLayerMask;

        private void Awake()
        {
            if (!NetworkServer.active) return;

            _enviromentLayerMask = LayerMask.GetMask("Enviroment");

            EventBus<OnGameStateChange>.Listen((data) =>
            {
                _timer = 0f;
                _active = data.state.phase.type == GamePhaseType.Match;
            });

            EventBus<OnBoxSpawn>.Listen((_) => _spawnedBoxesCounter++);
            EventBus<OnBoxDestroy>.Listen((_) => _spawnedBoxesCounter--);

            EventBus<HitEvent>.Listen(OnHit);

            EventBus<OnItemUsed>.Listen((data) =>
            {
                if (!data.fullyUsed) return;
                OnItemUsed(data.player);
            });
        }

        private void OnHit(HitEvent hitEvent)
        {
            if (hitEvent.source.layer != boxesLayer) return;
            if (hitEvent.target is not PlayerItemModule playerItemModule) return;

            if (playerItemModule.itemData.itemIndex == -1)
            {
                playerItemModule.PickRandomItem();
                NetworkServer.Destroy(hitEvent.source.gameObject);
            }
        }

        private void OnItemUsed(PlayerBase player)
        {
            for (int i = 0; i < maxNeedSpawnFails; i++)
            {
                var info = MapLoader.loadedMap.info;
                var playerShapePosition = new Vector3(player.transform.position.x, info.boundsMax.y, player.transform.position.z);
                var point = Vector3.Lerp(info.SelectRandomPointOnSpawnShape(), playerShapePosition, offsetTowardsPlayerFactor);
                if (TrySpawnBox(point)) break;
                Debug.Log($"Failed to spawn need box. Fail iteration: {i}");
            }
        }

        private void Update()
        {
            if (!_active) return;

            if (MapLoader.loadedMap == null || !MapLoader.loadedMap.scene.IsValid())
            {
                Debug.LogWarning("Box spawner cant function without a loaded map");
                _active = false;
                return;
            }

            HandleBoxSpawning();
        }

        private void HandleBoxSpawning()
        {
            var playerCount = MapLoader.loadedMap.players.Count;
            if (_spawnedBoxesCounter >= maxBoxesPerPlayer * playerCount) return;

            if (_timer <= 0f)
            {
                _timer = 1f / (ambientSpawnRate * playerCount);
                for (int i = 0; i < maxAmbientSpawnFails; i++)
                {
                    var point = MapLoader.loadedMap.info.SelectRandomPointOnSpawnShape();
                    if (TrySpawnBox(point)) break;
                    Debug.Log($"Failed to spawn ambient box. Fail iteration: {i}");
                }
            }
            else _timer -= Time.deltaTime;
        }

        private bool TrySpawnBox(Vector3 pointOnShape)
        {
            var origin = pointOnShape;
            var possibleSpawnPoints = new List<Vector3>();
            while (Physics.Raycast(origin, Vector3.down, out var hit, 200f, _enviromentLayerMask))
            {
                origin = hit.point + Vector3.down * 0.1f;

                if (Vector3.Angle(Vector3.up, hit.normal) > maxGroundAngle) continue;
                if (Physics.CheckSphere(hit.point + Vector3.up * spawnYOffset, spawnMargin, _enviromentLayerMask)) continue;

                possibleSpawnPoints.Add(hit.point);
            }

            if (possibleSpawnPoints.Count == 0) return false;

            var point = possibleSpawnPoints[Random.Range(0, possibleSpawnPoints.Count)];
            MapLoader.NetworkSpawnOnMap(boxPrefab, point + Vector3.up * spawnYOffset, Quaternion.identity);
            return true;
        }
    }
}