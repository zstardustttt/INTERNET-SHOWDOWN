using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Maps;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Boxes
{
    [Serializable]
    public struct BoxSpawnPositionsChunk
    {
        public Vector3 chunkCenter;
        public Vector3[] positions;
    }

    public class BoxSpawnBakeData : MonoBehaviour
    {
        public MapInfo mapInfo;

        [Header("Spawn positions bake properties")]
        public LayerMask obstacleLayerMask;
        public float maxGroundAngle = 70f;
        public float spawnMargin = 1.1f;
        public float spawnYOffset = 2f;
        public float spawnPositionsMinDistance = 1.99f;
        public float spawnPositionsChunkSize = 16f;
        public BoxSpawnPositionsChunk[] bakedBoxSpawnPositionsChunks;

        private void OnValidate()
        {
            if (!mapInfo)
            {
                Debug.LogError("Map info in box handler must be assigned!");
                return;
            }

            mapInfo.onSurfacePointsBaked.RemoveListener(BakeSpawnPoints);
            mapInfo.onSurfacePointsBaked.AddListener(BakeSpawnPoints);
        }

        [ContextMenu("Bake Spawn Points")]
        public void BakeSpawnPoints()
        {
            var output = new Dictionary<Vector3, List<Vector3>>();
            var positionsBuffer = new Vector3[mapInfo.surfacePoints.Length];
            var positionsBufferCount = 0;

            foreach (var surfacePoint in mapInfo.surfacePoints)
            {
                if (Vector3.Angle(Vector3.up, surfacePoint.normal) > maxGroundAngle) continue;

                var violatedDistance = false;
                var boxPosition = surfacePoint.position + Vector3.up * spawnYOffset;
                for (int i = 0; i < positionsBufferCount; i++)
                {
                    if (Vector3.Distance(boxPosition, positionsBuffer[i]) >= spawnPositionsMinDistance) continue;

                    violatedDistance = true;
                    break;
                }

                if (violatedDistance) continue;
                if (Physics.CheckSphere(boxPosition, spawnMargin, obstacleLayerMask)) continue;

                positionsBuffer[positionsBufferCount] = boxPosition;
                positionsBufferCount++;

                var chunkPosition = new Vector3
                (
                    Mathf.Round(boxPosition.x / spawnPositionsChunkSize) * spawnPositionsChunkSize,
                    Mathf.Round(boxPosition.y / spawnPositionsChunkSize) * spawnPositionsChunkSize,
                    Mathf.Round(boxPosition.z / spawnPositionsChunkSize) * spawnPositionsChunkSize
                );

                if (output.TryAdd(chunkPosition, new() { boxPosition })) continue;
                output[chunkPosition].Add(boxPosition);
            }

            bakedBoxSpawnPositionsChunks = output.Select(x => new BoxSpawnPositionsChunk()
            {
                chunkCenter = x.Key,
                positions = x.Value.ToArray()
            }).ToArray();
        }

        public Vector3 GetClosestSpawnPosition(Vector3 closestTo)
        {
            var closestChunk = -1;
            var closestChunkDistance = 9999f;
            for (int i = 0; i < bakedBoxSpawnPositionsChunks.Length; i++)
            {
                var dist = (bakedBoxSpawnPositionsChunks[i].chunkCenter - closestTo).sqrMagnitude;
                if (dist >= closestChunkDistance) continue;
                closestChunk = i;
                closestChunkDistance = dist;
            }

            if (closestChunk == -1) return Vector3.zero;
            var chunk = bakedBoxSpawnPositionsChunks[closestChunk];

            var closestPosition = Vector3.zero;
            var closestDistance = 9999f;
            foreach (var position in chunk.positions)
            {
                var dist = Vector3.Distance(position, closestTo);
                if (dist >= closestDistance) continue;
                closestPosition = position;
                closestDistance = dist;
            }

            return closestPosition;
        }

        public Vector3 GetRandomSpawnPosition()
        {
            var chunk = bakedBoxSpawnPositionsChunks[Random.Range(0, bakedBoxSpawnPositionsChunks.Length)];
            return chunk.positions[Random.Range(0, chunk.positions.Length)];
        }

        private void OnDrawGizmosSelected()
        {
            if (bakedBoxSpawnPositionsChunks == null) return;

            Gizmos.color = Color.yellow;
            foreach (var chunk in bakedBoxSpawnPositionsChunks)
            {
                Gizmos.DrawWireCube(chunk.chunkCenter, Vector3.one * spawnPositionsChunkSize);
                foreach (var spawnPosition in chunk.positions)
                {
                    Gizmos.DrawWireSphere(spawnPosition, 0.25f);
                }
            }
        }
    }
}