using System.Collections.Generic;
using Game.Core.Events;
using Game.Core.Maps;
using Game.Events.BoxSpawner;
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
        public float castMargin;
        public LayerMask layerMask;
        public int maxSpawnFails;

        private bool _active;
        private float _timer;

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

            _timer += Time.deltaTime;
            if (_timer >= 1f / (spawnRate * MapLoader.loadedMap.players.Count))
            {
                _timer = 0f;
                for (int i = 0; i < maxSpawnFails; i++)
                {
                    if (TrySpawnBox()) break;
                }
            }

            foreach (var (_, player) in MapLoader.loadedMap.players)
            {
                if (!player) continue;
                player.boxSpawnerObservedDelta = player.transform.position - player.boxSpawnerPreviousObservedPosition;

                if (player.itemData.itemIndex == -1 && PlayerBoxCheck(player, out var box))
                {
                    player.PickRandomItem();
                    NetworkServer.Destroy(box);
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
            var x = Random.Range(info.boxSpawnPlane.x, info.boxSpawnPlane.z);
            var z = Random.Range(info.boxSpawnPlane.y, info.boxSpawnPlane.w);

            var origin = info.transform.position + new Vector3(x, info.boundsMax.y, z);
            var possibleSpawnPoints = new List<Vector3>();
            while (Physics.Raycast(origin, Vector3.down, out var hit, 200f, layerMask))
            {
                possibleSpawnPoints.Add(hit.point);
                origin = hit.point + Vector3.down * 0.1f;
            }

            if (possibleSpawnPoints.Count == 0) return false;
            var point = possibleSpawnPoints[Random.Range(0, possibleSpawnPoints.Count)];
            var box = Instantiate(boxPrefab, point, Quaternion.identity, new InstantiateParameters() { scene = MapLoader.loadedMap.scene });
            NetworkServer.Spawn(box);

            return true;
        }

        /*
        Legacy solution
        private List<KinematicCharacterMotor> GetAllMotorsOnScene(Scene scene)
        {
            var output = new List<KinematicCharacterMotor>();
            foreach (var obj in scene.GetRootGameObjects())
            {
                if (obj.TryGetComponent(out KinematicCharacterMotor motor)) output.Add(motor);
            }

            return output;
        }
        */
    }
}