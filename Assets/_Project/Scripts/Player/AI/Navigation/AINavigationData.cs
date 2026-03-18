using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Game.Core.Maps;
using Game.Core.Player.Movement;
using Game.Player.AI.Navigation.Pathfinding;
using UnityEditor;
using UnityEngine;

namespace Game.Player.AI.Navigation
{
    [Serializable]
    public struct NavigationNodesChunk
    {
        public Vector3Int chunkCenter;
        public int[] nodeIndices;
    }

    [Serializable]
    public class NavigationNode
    {
        public List<NavigationLink> links;
        public Vector3 position;

        public NavigationNode(Vector3 position)
        {
            this.position = position;
            links = new();
        }
    }

    public struct NavigationNodeBakingData
    {
        public NavigationNode node;
        public int index;
        public bool discarded;

        public NavigationNodeBakingData(NavigationNode node, int index, bool discarded)
        {
            this.node = node;
            this.index = index;
            this.discarded = discarded;
        }
    }

    public class NavigationTile
    {
        public Vector2 position;
        public List<NavigationNodeBakingData> nodeDatas;

        public NavigationTile(Vector2 position)
        {
            this.position = position;
            nodeDatas = new();
        }
    }

    [Serializable]
    public struct NavigationLink
    {
        public int nodeIndex;
        public AIMovementDescriptor movement;
        public float cost;
    }

    public class AINavigationData : MonoBehaviour
    {
        public MapInfo mapInfo;

        [Header("Baking properties")]
        public PlayerMovementConfig config;
        public AINavigationDataContainer navigationDataContainer;
        public int nodesChunkSize = 16;
        public float maxAscendHeight = 10f;

        [Header("Debug")]
        public bool showChunkGizmos = true;
        public Transform debugStartPathPoint;
        public Transform debugEndPathPoint;
        public AIPath debugPath;

        public Pathfinder Pathfinder { get; private set; }

        private void OnValidate()
        {
            if (!mapInfo)
            {
                Debug.LogError("Map info in AI player map data must be assigned!");
                return;
            }

            mapInfo.onSurfacePointsBaked.RemoveListener(BakeNavigationNodes);
            mapInfo.onSurfacePointsBaked.AddListener(BakeNavigationNodes);
        }

        private void Awake()
        {
            InitPathfinder();
            navigationDataContainer.MapNavigationNodesChunks();
        }

        private void InitPathfinder()
        {
            Pathfinder = new(navigationDataContainer.bakedNavigationNodes, mapInfo.distanceBetweenSurfacePoints);
        }

        [ContextMenu("Bake Navigation Nodes")]
        public void BakeNavigationNodes()
        {
            if (!navigationDataContainer)
            {
                Debug.LogError("AI Navigation Data Container wasn't specified!");
                return;
            }

            var sizeX = mapInfo.surfaceTriangulationData.boundsMax.x - mapInfo.surfaceTriangulationData.boundsMin.x;
            var sizeY = mapInfo.surfaceTriangulationData.boundsMax.y - mapInfo.surfaceTriangulationData.boundsMin.y;

            var countX = (int)(sizeX / mapInfo.distanceBetweenSurfacePoints);
            var countY = (int)(sizeY / mapInfo.distanceBetweenSurfacePoints);

            var tiles = new NavigationTile[countX * countY];

            var nodes = new List<NavigationNode>();
            var chunks = new Dictionary<Vector3Int, List<int>>();
            foreach (var surfacePoint in mapInfo.surfacePoints)
            {
                if (Vector3.Angle(Vector3.up, surfacePoint.normal) > config.maxGroundAngle) continue;
                var startPoint = surfacePoint.position + Vector3.up * (config.maxStepHeight + config.colliderCapsuleRadius);
                var endPoint = surfacePoint.position + Vector3.up * (config.colliderCapsuleHeight - config.colliderCapsuleRadius);
                var discarded = Physics.CheckCapsule(startPoint, endPoint, config.colliderCapsuleRadius, config.stableGroundLayers);

                var tile = tiles[surfacePoint.gridIndex];
                if (tile == null)
                {
                    tile = new NavigationTile(new Vector2(surfacePoint.position.x, surfacePoint.position.z));
                    tiles[surfacePoint.gridIndex] = tile;
                }

                var node = new NavigationNode(surfacePoint.position);
                var nodeIndex = nodes.Count;
                tile.nodeDatas.Add(new(node, nodeIndex, discarded));

                if (discarded) continue;
                nodes.Add(node);

                var chunkPosition = new Vector3Int
                (
                    Mathf.RoundToInt(node.position.x / nodesChunkSize) * nodesChunkSize,
                    Mathf.RoundToInt(node.position.y / nodesChunkSize) * nodesChunkSize,
                    Mathf.RoundToInt(node.position.z / nodesChunkSize) * nodesChunkSize
                );

                if (chunks.TryAdd(chunkPosition, new() { nodeIndex })) continue;
                chunks[chunkPosition].Add(nodeIndex);
            }

            for (int i = 0; i < tiles.Length; i++)
            {
                var tile = tiles[i];
                if (tile == null) continue;
                var neighbors = GetNeighboringTiles(i, tiles, countY);

                var selfNodeAbove = new NavigationNodeBakingData(null, -1, false);
                foreach (var selfNodeData in tile.nodeDatas)
                {
                    if (selfNodeData.discarded)
                    {
                        selfNodeAbove = selfNodeData;
                        continue;
                    }

                    foreach (var neighbor in neighbors)
                    {
                        if (neighbor == null) continue;
                        var neighborDistance = Vector2.Distance
                        (
                            new(selfNodeData.node.position.x, selfNodeData.node.position.z),
                            neighbor.position
                        );

                        var canFall = true;
                        var fallNeighborNode = new NavigationNodeBakingData(null, -1, false);
                        var otherNodeAbove = new NavigationNodeBakingData(null, -1, false);
                        foreach (var otherNodeData in neighbor.nodeDatas)
                        {
                            var delta = otherNodeData.node.position - selfNodeData.node.position;
                            if (Mathf.Abs(delta.y) < config.maxStepHeight)
                            {
                                canFall = false;

                                var startPoint = selfNodeData.node.position + Vector3.up * (config.maxStepHeight + config.colliderCapsuleRadius);
                                var endPoint = selfNodeData.node.position + Vector3.up * (config.colliderCapsuleHeight - config.colliderCapsuleRadius);
                                var direction = delta.normalized;
                                var distance = delta.magnitude;
                                if (Physics.CapsuleCast(startPoint, endPoint, config.colliderCapsuleRadius, direction, distance, config.stableGroundLayers)) continue;

                                selfNodeData.node.links.Add(new()
                                {
                                    cost = neighborDistance,
                                    movement = new AIMovementDescriptor(selfNodeData.node.position, otherNodeData.node.position, AIMovementType.Flat),
                                    nodeIndex = otherNodeData.index
                                });
                            }
                            else if (delta.y > 0f && delta.y <= maxAscendHeight
                                && (selfNodeAbove.index == -1 || selfNodeAbove.node.position.y > otherNodeData.node.position.y + config.colliderCapsuleHeight))
                            {
                                selfNodeData.node.links.Add(new()
                                {
                                    cost = neighborDistance,
                                    movement = new AIMovementDescriptor(selfNodeData.node.position, otherNodeData.node.position, AIMovementType.Ascend),
                                    nodeIndex = otherNodeData.index
                                });
                            }
                            else if (selfNodeData.node.position.y > otherNodeData.node.position.y
                                && (fallNeighborNode.index == -1 || otherNodeData.node.position.y > fallNeighborNode.node.position.y)
                                && (otherNodeAbove.index == -1 || otherNodeAbove.node.position.y > selfNodeData.node.position.y + config.colliderCapsuleHeight))
                            {
                                fallNeighborNode = otherNodeData;
                            }

                            otherNodeAbove = otherNodeData;
                        }

                        if (canFall && fallNeighborNode.index != -1 && !fallNeighborNode.discarded)
                        {
                            selfNodeData.node.links.Add(new()
                            {
                                cost = neighborDistance,
                                movement = new AIMovementDescriptor(selfNodeData.node.position, fallNeighborNode.node.position, AIMovementType.Descend),
                                nodeIndex = fallNeighborNode.index
                            });
                        }
                    }

                    selfNodeAbove = selfNodeData;
                }
            }

            navigationDataContainer.bakedNavigationNodes = nodes.ToArray();
            navigationDataContainer.bakedNavigationNodesChunks = chunks.Select(x => new NavigationNodesChunk()
            {
                chunkCenter = x.Key,
                nodeIndices = x.Value.ToArray()
            }).ToArray();

#if UNITY_EDITOR
            EditorUtility.SetDirty(navigationDataContainer);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private NavigationTile[] GetNeighboringTiles(int idx, NavigationTile[] tiles, int countY)
        {
            var neighborPXIdx = idx + countY;
            var neighborNXIdx = idx - countY;
            var neighborPYIdx = idx + 1;
            var neighborNYIdx = idx - 1;
            var neighborPXPYIdx = idx + countY + 1;
            var neighborPXNYIdx = idx + countY - 1;
            var neighborNXPYIdx = idx - countY + 1;
            var neighborNXNYIdx = idx - countY - 1;

            return new NavigationTile[8]
            {
                neighborPXIdx < 0 || neighborPXIdx >= tiles.Length ? null : tiles[neighborPXIdx],
                neighborNXIdx < 0 || neighborNXIdx >= tiles.Length ? null : tiles[neighborNXIdx],
                neighborPYIdx < 0 || neighborPYIdx >= tiles.Length ? null : tiles[neighborPYIdx],
                neighborNYIdx < 0 || neighborNYIdx >= tiles.Length ? null : tiles[neighborNYIdx],
                neighborPXPYIdx < 0 || neighborPXPYIdx >= tiles.Length ? null : tiles[neighborPXPYIdx],
                neighborPXNYIdx < 0 || neighborPXNYIdx >= tiles.Length ? null : tiles[neighborPXNYIdx],
                neighborNXPYIdx < 0 || neighborNXPYIdx >= tiles.Length ? null : tiles[neighborNXPYIdx],
                neighborNXNYIdx < 0 || neighborNXNYIdx >= tiles.Length ? null : tiles[neighborNXNYIdx],
            };
        }

        public int GetClosestNodeIndex(Vector3 closestTo)
        {
            var closestChunkPosition = new Vector3Int
            (
                Mathf.RoundToInt(closestTo.x / nodesChunkSize) * nodesChunkSize,
                Mathf.RoundToInt(closestTo.y / nodesChunkSize) * nodesChunkSize,
                Mathf.RoundToInt(closestTo.z / nodesChunkSize) * nodesChunkSize
            );

            if (!navigationDataContainer.NavigationNodesChunkMap.TryGetValue(closestChunkPosition, out var closestChunk))
                return -1;

            return SearchForClosestInChunk(closestChunk, closestTo).nodeIndex;

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (int nodeIndex, float distance) SearchForClosestInChunk(NavigationNodesChunk chunk, Vector3 closestTo)
        {
            var closestNodeIndex = -1;
            var closestDistance = 9999f;
            foreach (var nodeIndex in chunk.nodeIndices)
            {
                var node = navigationDataContainer.bakedNavigationNodes[nodeIndex];
                var dist = Vector3.Distance(node.position, closestTo);
                if (dist >= closestDistance) continue;
                closestNodeIndex = nodeIndex;
                closestDistance = dist;
            }

            return (closestNodeIndex, closestDistance);
        }

        [ContextMenu("Debug Path")]
        private void DebugPath()
        {
            InitPathfinder();
            navigationDataContainer.MapNavigationNodesChunks();
            debugPath = Pathfinder.FindPath(GetClosestNodeIndex(debugStartPathPoint.position), GetClosestNodeIndex(debugEndPathPoint.position));
        }

        private void OnDrawGizmosSelected()
        {
            if (!navigationDataContainer) return;

            if (navigationDataContainer.bakedNavigationNodes != null)
            {
                foreach (var node in navigationDataContainer.bakedNavigationNodes)
                {
                    Gizmos.color = Color.softBlue;
                    Gizmos.DrawWireSphere(node.position, 0.25f);

                    if (node.links == null) continue;

                    foreach (var link in node.links)
                    {
                        var lod = Gizmos.CalculateLOD((node.position + navigationDataContainer.bakedNavigationNodes[link.nodeIndex].position) / 2f, 0.25f);
                        if (lod == 0f) continue;

                        Gizmos.color = link.movement.GetGizmosColor() * lod;
                        Gizmos.DrawLine(node.position, navigationDataContainer.bakedNavigationNodes[link.nodeIndex].position);
                    }
                }
            }

            if (navigationDataContainer.bakedNavigationNodesChunks != null && showChunkGizmos)
            {
                foreach (var chunk in navigationDataContainer.bakedNavigationNodesChunks)
                {
                    Gizmos.color = Color.darkBlue;
                    Gizmos.DrawWireCube(chunk.chunkCenter, Vector3.one * nodesChunkSize);
                }
            }

            if (debugPath != null)
            {
                Gizmos.color = Color.yellow;
                foreach (var movement in debugPath.rawPath)
                {
                    Gizmos.DrawWireSphere(movement.startPosition, 0.35f);
                    Gizmos.DrawLine(movement.startPosition, movement.endPosition);
                }

                Gizmos.color = Color.green;
                foreach (var movement in debugPath.mergedPath)
                {
                    Gizmos.DrawWireSphere(movement.startPosition, 0.5f);
                    Gizmos.DrawLine(movement.startPosition, movement.endPosition);
                }
            }
        }
    }
}