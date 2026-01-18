using System.Collections.Generic;
using Game.Core.Events;
using Game.Core.Maps;
using Game.Events.BoxSpawner;
using Mirror;
using UnityEngine;

namespace Game.Systems
{
    // This object is only active on the server
    public class BoxSpawner : MonoBehaviour
    {
        public GameObject boxPrefab;
        public float spawnRate;
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