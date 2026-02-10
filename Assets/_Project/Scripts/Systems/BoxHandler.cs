using System.Collections.Generic;
using Game.Core.Events;
using Game.Core.Maps;
using Game.Events.Boxes;
using Game.Player;
using Mirror;
using UnityEngine;

namespace Game.Systems
{
    // This object is only active on the server
    public class BoxHandler : MonoBehaviour
    {
        public LayerMask playerBoxCheckLayerMask;
        public GameObject boxPrefab;
        public float spawnRate;
        public int maxBoxesPerPlayer;
        public float castMargin;
        public LayerMask layerMask;

        [Header("Box spawning settings")]
        public int maxSpawnFails;
        public float maxGroundAngle;
        public float spawnMargin;
        public float spawnYOffset;

        private bool _active;
        private float _timer;
        private int _spawnedBoxesCount;

        private void Awake()
        {
            EventBus<SetBoxSpawnerActive>.Listen((data) => _active = data.active);
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
            HandleBoxPicking();
        }

        private void HandleBoxSpawning()
        {
            var playerCount = MapLoader.loadedMap.players.Count;
            if (_spawnedBoxesCount >= maxBoxesPerPlayer * playerCount) return;

            if (_timer <= 0f)
            {
                _timer = 1f / (spawnRate * playerCount);
                for (int i = 0; i < maxSpawnFails; i++)
                {
                    if (TrySpawnBox())
                    {
                        _spawnedBoxesCount++;
                        break;
                    }
                    Debug.Log($"Failed to spawn box. Fail iteration: {i}");
                }
            }
            else _timer -= Time.deltaTime;
        }

        private void HandleBoxPicking()
        {
            foreach (var (_, player) in MapLoader.loadedMap.players)
            {
                if (!player || player.dead) continue;
                player.boxSpawnerObservedDelta = player.transform.position - player.boxSpawnerPreviousObservedPosition;

                if (player.itemData.itemIndex == -1 && PlayerBoxCheck(player, out var box))
                {
                    player.PickRandomItem();
                    NetworkServer.Destroy(box);
                    _spawnedBoxesCount--;
                }

                player.boxSpawnerPreviousObservedPosition = player.transform.position;
            }
        }

        private bool PlayerBoxCheck(PlayerBase player, out GameObject box)
        {
            var radius = player.motor.Capsule.radius;

            var pos = player.boxSpawnerPreviousObservedPosition;
            var p1 = pos + Vector3.up * radius;
            var p2 = pos + Vector3.up * (player.motor.Capsule.height - radius);

            var velDir = player.boxSpawnerObservedDelta.normalized;
            var delta = player.boxSpawnerObservedDelta.magnitude + castMargin;

            var overlaps = Physics.OverlapCapsule(p1, p2, radius, playerBoxCheckLayerMask, QueryTriggerInteraction.Collide);
            foreach (var overlap in overlaps)
            {
                box = overlap.gameObject;
                return true;
            }

            if (!Physics.CapsuleCast(p1, p2, radius, velDir, out var hit, delta, playerBoxCheckLayerMask, QueryTriggerInteraction.Collide))
            {
                box = null;
                return false;
            }

            box = hit.collider.gameObject;
            return true;
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
            while (Physics.Raycast(origin, Vector3.down, out var hit, 200f, layerMask))
            {
                origin = hit.point + Vector3.down * 0.1f;

                if (Vector3.Angle(Vector3.up, hit.normal) > maxGroundAngle) continue;
                if (Physics.CheckSphere(hit.point + Vector3.up * spawnYOffset, spawnMargin, layerMask)) continue;

                possibleSpawnPoints.Add(hit.point);
            }

            if (possibleSpawnPoints.Count == 0) return false;

            var point = possibleSpawnPoints[Random.Range(0, possibleSpawnPoints.Count)];
            var box = Instantiate(boxPrefab, point + Vector3.up * spawnYOffset, Quaternion.identity, new InstantiateParameters() { scene = MapLoader.loadedMap.scene });
            NetworkServer.Spawn(box);

            return true;
        }
    }
}