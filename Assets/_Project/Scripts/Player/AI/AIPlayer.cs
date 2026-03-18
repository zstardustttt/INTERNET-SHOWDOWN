using UnityEngine;
using Mirror;
using Game.Core.Player;
using System;
using Random = UnityEngine.Random;
using Game.Core.Player.Movement;
using Game.Core.Maps;
using Game.Boxes;
using Game.Player.AI.Navigation;
using System.Collections.Generic;

namespace Game.Player.AI
{
    [RequireComponent(typeof(PlayerCore))]
    public class AIPlayer : NetworkBehaviour, IPlayerMovementController
    {
        public PlayerCore player;
        private GameObject _portal;
        private AINavigationData navigationData;

        private Transform _target;
        private List<AIMovementDescriptor> _path;
        private int _currentNode;
        private float _nodeFollowTimer;

        protected override void OnValidate()
        {
            base.OnValidate();

            if (Application.isPlaying) return;
            player = GetComponent<PlayerCore>();
        }

        public override void OnStartServer()
        {
            _path = new();

            player.HandleThisPlayer(new()
            {
                name = $"Stupid Clanker {Random.Range(1, 100)}",
                guid = Guid.NewGuid()
            });

            player.onLocalTriggerEnter.AddListener((collider) =>
            {
                if (!collider.CompareTag("Portal")) return;

                if (!MapLoader.TryMoveGameObjectToMap(gameObject))
                {
                    Debug.LogWarning($"AI {player.Identification.name} wanted to load into unloaded map or has already loaded that map");
                    return;
                }

                player.movementModule.ServerMove(MapLoader.loadedMap.info.GetRandomSpawnPoint());
            });

            player.movementModule.controller = this;
        }

        public PlayerMovementInputs GetInputs(float deltaTime)
        {
            if (player.State == PlayerState.InLobby)
                return GetLobbyInputs(deltaTime);

            if (player.State == PlayerState.InMatch)
                return GetMatchInputs(deltaTime);

            return new();
        }

        private PlayerMovementInputs GetLobbyInputs(float deltaTime)
        {
            if (!_portal)
                _portal = GameObject.FindGameObjectWithTag("Portal");

            if (!_portal || !_portal.activeInHierarchy) return new();

            LookAt(_portal.transform.position);
            return new()
            {
                move = new(0f, 1f),
            };
        }

        private PlayerMovementInputs GetMatchInputs(float deltaTime)
        {
            if (!navigationData)
            {
                navigationData = FindFirstObjectByType<AINavigationData>(FindObjectsInactive.Include);
                if (!navigationData)
                {
                    Debug.LogWarning("AI Navigation data can't be found!");
                    return default;
                }
            }

            if (player.itemModule.Item)
            {
                //LookAt(transform.position + Vector3.up * 100f);
                //player.itemModule.TryUseItem(false);
                player.itemModule.ResetItem();
                player.itemModule.InvokeItemUseEvents(true);
            }

            if (!_target || _nodeFollowTimer <= 0f)
            {
                GetPathToClosestBox();
                if (!_target) return default;
            }

            return PathUpdate(deltaTime);
        }

        private bool _prevWishDashing;
        private float _walledTimer;

        // Overshoot detection: Dot product between normalized(startpos -> endpos) and (playerpos -> endpos)
        // [1; 0] - following, (0; -1] - overshoot
        // TODO: use dash to ascend even higher
        private PlayerMovementInputs PathUpdate(float deltaTime)
        {
            if (_path == null || _currentNode >= _path.Count)
            {
                _target = null;
                return default;
            }

            _nodeFollowTimer -= deltaTime;
            var currentNode = _path[_currentNode];

            if (currentNode.type == AIMovementType.Ascend && transform.position.y > currentNode.endPosition.y)
            {
                _currentNode++;
                _nodeFollowTimer = 3f;
            }
            else if (Vector3.Dot(currentNode.Direction, (currentNode.endPosition - transform.position).normalized) < 0)
            {
                _currentNode++;
                _nodeFollowTimer = 3f;
            }

            _prevWishDashing = !_prevWishDashing;
            if (player.movementModule.Walled) _walledTimer += deltaTime;
            else _walledTimer = 0f;

            LookAt(currentNode.endPosition + Vector3.up * player.verticalOrientation.localPosition.y);
            return new()
            {
                move = Vector2.up,
                wishDashing = _prevWishDashing && !player.movementModule.Walled && (Vector3.Distance(transform.position, currentNode.endPosition) > 7f || _currentNode == _path.Count - 1),
                wishJumping = currentNode.type == AIMovementType.Ascend && (player.movementModule.motor.GroundingStatus.IsStableOnGround || player.movementModule.Jumping),
                wishGroundSlam = _walledTimer > 0.5f
            };
        }

        // TODO: offset start position of ascending nodes a few units back
        private void GetPathToClosestBox()
        {
            var boxes = FindObjectsByType<ItemBox>(FindObjectsSortMode.None);
            if (boxes.Length == 0) return;

            var toNodeIndices = new int[boxes.Length];
            for (int i = 0; i < boxes.Length; i++)
            {
                toNodeIndices[i] = navigationData.GetClosestNodeIndex(boxes[i].transform.position - Vector3.up * 2f);
            }

            var fromIdx = navigationData.GetClosestNodeIndex(transform.position);
            if (fromIdx == -1) return;

            var path = navigationData.Pathfinder.FindClosestPathOutOf(fromIdx, toNodeIndices, out var chosenIndex);
            if (path == null) return;

            var startCount = path.mergedPath.Count;
            _target = boxes[chosenIndex].transform;

            var currentIdx = 0;
            var castCapsuleRadius = player.movementModule.config.colliderCapsuleRadius;
            var castLayerMask = player.movementModule.config.stableGroundLayers;
            var castCapsulePoint1 = Vector3.up * (player.movementModule.config.maxStepHeight + castCapsuleRadius);
            var castCapsulePoint2 = Vector3.up * (player.movementModule.config.colliderCapsuleHeight - castCapsuleRadius);
            while (currentIdx + 1 < path.mergedPath.Count)
            {
                var currentNode = path.mergedPath[currentIdx];
                var nextNode = path.mergedPath[currentIdx + 1];

                AIMovementDescriptor mergedNode;
                bool castForDescend;
                if (currentNode.type == AIMovementType.Descend && nextNode.type == AIMovementType.Flat)
                {
                    mergedNode = new(currentNode.startPosition, nextNode.endPosition, AIMovementType.Descend);
                    castForDescend = true;
                }
                else if (currentNode.type == AIMovementType.Flat && nextNode.type == AIMovementType.Flat)
                {
                    mergedNode = new(currentNode.startPosition, nextNode.endPosition, AIMovementType.Flat);
                    castForDescend = false;
                }
                else
                {
                    currentIdx++;
                    continue;
                }

                var point1 = mergedNode.startPosition + castCapsulePoint1;
                var point2 = mergedNode.startPosition + castCapsulePoint2;
                if (castForDescend)
                {
                    // TODO: non alloc
                    var hits = Physics.CapsuleCastAll(point1, point2, castCapsuleRadius, mergedNode.Direction, mergedNode.Length, castLayerMask);
                    var canMerge = true;
                    foreach (var hit in hits)
                    {
                        if (Vector3.Angle(Vector3.up, hit.normal) > player.movementModule.config.maxGroundAngle)
                        {
                            canMerge = false;
                            break;
                        }
                    }

                    if (!canMerge)
                    {
                        currentIdx++;
                        continue;
                    }
                }
                else if (Physics.CapsuleCast(point1, point2, castCapsuleRadius, mergedNode.Direction, mergedNode.Length, castLayerMask))
                {
                    currentIdx++;
                    continue;
                }

                path.mergedPath.RemoveAt(currentIdx);
                path.mergedPath[currentIdx] = mergedNode;
            }

            Debug.Log($"Optimized from {startCount} to {path.mergedPath.Count}");

            _path.Clear();
            foreach (var node in path.mergedPath)
            {
                _path.Add(node);
            }

            _currentNode = 0;
            _nodeFollowTimer = 3f;
        }

        private void LookAt(Vector3 position)
        {
            var dir = (position - player.verticalOrientation.position).normalized;
            player.horizontalOrientation.localEulerAngles = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg * Vector3.up;
            player.verticalOrientation.localEulerAngles = Mathf.Asin(dir.y) * Mathf.Rad2Deg * Vector3.left;
        }

        private void OnDrawGizmos()
        {
            if (_path == null || _currentNode >= _path.Count) return;

            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(_path[_currentNode].startPosition + Vector3.up, 0.5f);

            foreach (var movement in _path)
            {
                Gizmos.color = movement.GetGizmosColor();
                Gizmos.DrawWireSphere(movement.startPosition, 0.5f);
                Gizmos.DrawLine(movement.startPosition, movement.endPosition);
            }
        }
    }
}