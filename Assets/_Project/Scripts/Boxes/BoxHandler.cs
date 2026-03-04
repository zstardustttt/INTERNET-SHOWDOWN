using System.Collections.Generic;
using Game.Boxes.Events;
using Game.Core.Events;
using Game.Core.Hits;
using Game.Core.Hits.Events;
using Game.Core.Maps;
using Game.GameLoop;
using Game.GameLoop.Events;
using Game.Player;
using Mirror;
using UnityEngine;

namespace Game.Boxes
{
    // This object is only active on the server
    public class BoxHandler : MonoBehaviour
    {
        public GameObject boxPrefab;
        public HitLayer boxesLayer;
        public float spawnRate;
        public int maxBoxesPerPlayer;

        [Header("Box spawning settings")]
        public int maxSpawnFails;
        public float maxGroundAngle;
        public float spawnMargin;
        public float spawnYOffset;

        private bool _active;
        private float _timer;
        private int _spawnedBoxesCounter;
        private LayerMask _enviromentLayerMask;

        private void Awake()
        {
            _enviromentLayerMask = LayerMask.GetMask("Enviroment");

            EventBus<OnGameStateChange>.Listen((data) =>
            {
                _timer = 0f;
                _active = data.state.phase.type == GamePhaseType.Match;
            });

            EventBus<OnBoxSpawn>.Listen((_) => _spawnedBoxesCounter++);
            EventBus<OnBoxDestroy>.Listen((_) => _spawnedBoxesCounter--);

            EventBus<HitEvent>.Listen(OnHit);
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
                _timer = 1f / (spawnRate * playerCount);
                for (int i = 0; i < maxSpawnFails; i++)
                {
                    if (TrySpawnBox()) break;
                    Debug.Log($"Failed to spawn box. Fail iteration: {i}");
                }
            }
            else _timer -= Time.deltaTime;
        }

        private bool TrySpawnBox()
        {
            var info = MapLoader.loadedMap.info;

            var probabilityOffset = 0f;
            var selectedTriangleIdx = -1;
            var triangleSelectionRandom = Random.value;
            for (int i = 0; i < info.boxSpawnShapeTriangulationData.triangles.Length; i++)
            {
                var triangle = info.boxSpawnShapeTriangulationData.triangles[i];
                var areaRatio = triangle.area / info.boxSpawnShapeTriangulationData.totalArea;
                if (triangleSelectionRandom >= probabilityOffset && triangleSelectionRandom < probabilityOffset + areaRatio)
                {
                    selectedTriangleIdx = i;
                    break;
                }
                probabilityOffset += areaRatio;
            }

            if (selectedTriangleIdx == -1) return false;
            var selectedTriangle = info.boxSpawnShapeTriangulationData.triangles[selectedTriangleIdx];

            var triangleOrigin = Vector2.Min(selectedTriangle.a, Vector2.Min(selectedTriangle.b, selectedTriangle.c));
            var relativeA = selectedTriangle.a - triangleOrigin;
            var relativeB = selectedTriangle.b - triangleOrigin;
            var relativeC = selectedTriangle.c - triangleOrigin;

            var trianglePointRandom1 = Random.value;
            var trianglePointRandom2 = Random.value;
            var triangleU = 1f - Mathf.Sqrt(trianglePointRandom1);
            var triangleV = Mathf.Sqrt(trianglePointRandom1) * (1f - trianglePointRandom2);
            var triangleW = 1f - triangleU - triangleV;
            var relativeRandomPoint = triangleU * relativeA + triangleV * relativeB + triangleW * relativeC;
            var randomPoint = relativeRandomPoint + triangleOrigin;

            var origin = info.transform.position + new Vector3(randomPoint.x, info.boundsMax.y, randomPoint.y);
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