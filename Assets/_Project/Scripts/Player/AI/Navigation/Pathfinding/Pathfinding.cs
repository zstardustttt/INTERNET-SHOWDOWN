using System;
using System.Collections.Generic;
using Game.Player.AI.Navigation.Pathfinding.Heap;
using UnityEngine;

namespace Game.Player.AI.Navigation.Pathfinding
{
    public class AIPath
    {
        public List<AIMovementDescriptor> rawPath;
        public List<AIMovementDescriptor> mergedPath;
        public float totalCost;
    }

    public class PathfindingNodeData : IComparable<PathfindingNodeData>
    {
        public NavigationNode navigationNode;
        public int index;

        public float gCost;
        public float hCost;
        public float fCost;
        public PathfindingNodeData parent;
        public AIMovementDescriptor parentMovement;
        public int heapIndex;
        public bool closed;

        public PathfindingNodeData(NavigationNode node, int index)
        {
            navigationNode = node;
            this.index = index;
            Reset();
        }

        public void Update(float gCost, float hCost, PathfindingNodeData parent, AIMovementDescriptor parentMovement)
        {
            this.gCost = gCost;
            this.hCost = hCost;
            fCost = gCost + hCost;

            this.parent = parent;
            this.parentMovement = parentMovement;
        }

        public void Reset()
        {
            gCost = 0;
            hCost = 0;
            fCost = 0;
            parent = null;
            parentMovement = default;
            heapIndex = -1;
            closed = false;
        }

        public int CompareTo(PathfindingNodeData other)
        {
            int output = fCost.CompareTo(other.fCost);
            if (output == 0) output = hCost.CompareTo(other.hCost);
            return output;
        }
    }

    public class Pathfinder
    {
        public PathfindingNodeData[] pathfindingNodeDatas;
        public float minimumLinkCost;

        private readonly PathfindingHeap _openSet;
        private readonly int[] _closedSet;

        public Pathfinder(NavigationNode[] nodes, float minimumLinkCost)
        {
            pathfindingNodeDatas = new PathfindingNodeData[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                pathfindingNodeDatas[i] = new(nodes[i], i);
            }

            this.minimumLinkCost = minimumLinkCost;
            _openSet = new(nodes.Length);
            _closedSet = new int[nodes.Length];
        }

        public AIPath FindClosestPathOutOf(int fromNodeIndex, int[] toNodeIndices, out int chosenIndex)
        {
            AIPath closestPath = null;
            var outputChosenIndex = -1;
            for (int i = 0; i < toNodeIndices.Length; i++)
            {
                // ПОХУЙ, НАХУЙ
                if (toNodeIndices[i] == -1) continue;

                var path = FindPath(fromNodeIndex, toNodeIndices[i]);
                if (path == null) continue;

                if (closestPath == null || closestPath.totalCost > path.totalCost)
                {
                    closestPath = path;
                    outputChosenIndex = i;
                }
            }

            chosenIndex = outputChosenIndex;
            return closestPath;
        }

        public AIPath FindPath(int fromNodeIndex, int toNodeIndex)
        {
            AIPath result = null;

            var fromNode = pathfindingNodeDatas[fromNodeIndex];
            var toNode = pathfindingNodeDatas[toNodeIndex];
            _openSet.Clear(fromNode);
            var closedSetCount = 0;

            while (_openSet.count > 0)
            {
                var currentNode = _openSet.Pop();
                _closedSet[closedSetCount] = currentNode.index;
                closedSetCount++;
                currentNode.closed = true;

                if (currentNode.index == toNodeIndex)
                {
                    // Found the path!
                    var rawOutput = new List<AIMovementDescriptor>();
                    var mergedOutput = new List<AIMovementDescriptor>();
                    var totalGCost = currentNode.gCost;

                    var currentPathNode = currentNode;
                    AIMovementDescriptor currentMovement = default;
                    AIMovementDescriptor previousMovement;
                    while (currentPathNode.parent != null)
                    {
                        previousMovement = currentMovement;
                        currentMovement = currentPathNode.parentMovement;
                        currentPathNode = currentPathNode.parent;

                        rawOutput.Insert(0, currentMovement);

                        if (mergedOutput.Count <= 1)
                        {
                            mergedOutput.Insert(0, currentMovement);
                            continue;
                        }

                        var firstMergedMovement = mergedOutput[0];
                        if (currentMovement.TryMerge(firstMergedMovement, out var merged))
                        {
                            if (Vector3.Angle(previousMovement.Direction, currentMovement.Direction) <= 1f)
                                mergedOutput[0] = merged;
                            else mergedOutput.Insert(0, currentMovement);
                        }
                        else mergedOutput.Insert(0, currentMovement);
                    }
                    rawOutput.Insert(0, currentMovement);

                    result = new()
                    {
                        rawPath = rawOutput,
                        mergedPath = mergedOutput,
                        totalCost = totalGCost,
                    };
                    break;
                }

                for (int i = 0; i < currentNode.navigationNode.links.Count; i++)
                {
                    var navigationLink = currentNode.navigationNode.links[i];
                    var linkedNode = pathfindingNodeDatas[navigationLink.nodeIndex];
                    if (linkedNode.closed) continue;

                    var updatedLinkedNodeGCost = currentNode.gCost + navigationLink.cost;
                    var contains = _openSet.Contains(linkedNode);
                    if (updatedLinkedNodeGCost < linkedNode.gCost || !contains)
                    {
                        linkedNode.Update
                        (
                            updatedLinkedNodeGCost,
                            Vector3.Distance(linkedNode.navigationNode.position, toNode.navigationNode.position) * minimumLinkCost,
                            currentNode,
                            navigationLink.movement
                        );

                        if (!contains)
                            _openSet.Push(linkedNode);
                    }
                }
            }

            for (int i = 0; i < _openSet.count; i++)
            {
                _openSet.items[i].Reset();
            }

            for (int i = 0; i < closedSetCount; i++)
            {
                pathfindingNodeDatas[_closedSet[i]].Reset();
            }

            return result;
        }
    }
}