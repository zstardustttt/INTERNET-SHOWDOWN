using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Game.Player;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Projectiles.Psycheshock.LinkedShurikens
{
    public class LinkedShurikensManager : NetworkBehaviour
    {
        [Header("Objects")]
        public ShurikenLink shurikenLinkPrefab;

        [Header("Properties")]
        public float pointsPerUnit;
        public int maxLinksCount;

        private List<LinkedShurikenProjectile> _projectiles;
        private List<ShurikenLink> _shurikenLinks;

        [SyncVar(hook = nameof(OnActiveLinksCountChanged))] public int activeLinksCount;

        private void OnActiveLinksCountChanged(int old, int _new)
        {
            for (int i = 0; i < _shurikenLinks.Count; i++)
            {
                _shurikenLinks[i].gameObject.SetActive(i < _new);
            }
        }

        private void Awake()
        {
            _projectiles = new();
            _shurikenLinks = new();

            for (int i = 0; i < maxLinksCount; i++)
            {
                var link = Instantiate(shurikenLinkPrefab.gameObject, transform).GetComponent<ShurikenLink>();
                link.lineRenderer.positionCount = 2;
                _shurikenLinks.Add(link);

                link.gameObject.SetActive(false);
            }
        }

        public void SetupAuthorAndFamily(PlayerBase author, Guid family)
        {
            foreach (var link in _shurikenLinks)
            {
                link.damageSource.author = author;
                link.damageSource.family = family;
                link.damageSource.beforeHitScan.AddListener(() =>
                {
                    if (!link.startProj || !link.endProj) return;
                    var start = link.startPos;
                    var end = link.endPos;

                    link.hitEntity.transform.position = (start + end) / 2f;
                    link.hitEntity.transform.up = (start - end).normalized;
                    link.hitEntity.capsuleCollider.height = (start - end).magnitude;
                });
            }
        }

        private void Update()
        {
            var localPlayerTransform = NetworkClient.localPlayer.transform;
            foreach (var link in _shurikenLinks)
            {
                if (!localPlayerTransform) break;
                if (!link.gameObject.activeInHierarchy) continue;

                // Electricity effect
                var projectileDistance = Vector3.Distance(link.startPos, link.endPos);
                var pointsCount = Mathf.Max(Mathf.CeilToInt(projectileDistance * pointsPerUnit), 2);

                link.lineRenderer.positionCount = pointsCount;
                link.lineRenderer.SetPosition(0, link.startPos);
                link.lineRenderer.SetPosition(pointsCount - 1, link.endPos);

                for (int i = 1; i < pointsCount - 1; i++)
                {
                    var pointOnLine = Vector3.Lerp(link.startPos, link.endPos, (float)i / pointsCount);
                    var offset = Random.insideUnitSphere * 0.5f;
                    link.lineRenderer.SetPosition(i, pointOnLine + offset);
                }

                // Move audio source
                var middlePoint = (link.endPos + link.startPos) / 2f;
                var lineDirection = (link.endPos - link.startPos).normalized;
                var targetDirection = localPlayerTransform.position - middlePoint;
                var projection = Vector3.ClampMagnitude(Vector3.Project(targetDirection, lineDirection), (link.endPos - link.startPos).magnitude / 2f);
                link.audioSource.transform.position = middlePoint + projection;
            }

            if (!NetworkServer.active) return;
            if (_projectiles.Count == 0) return;

            var previousProjectile = _projectiles[^1];
            var starts = new List<Vector3>();
            var ends = new List<Vector3>();
            for (int i = 0; i < _projectiles.Count; i++)
            {
                var projectile = _projectiles[i];

                var start = previousProjectile.transform.position;
                var end = projectile.transform.position;
                starts.Add(start);
                ends.Add(end);

                var link = _shurikenLinks[i];
                link.startProj = previousProjectile;
                link.endProj = projectile;
                link.startPos = start;
                link.endPos = end;

                previousProjectile = projectile;
            }

            if (NetworkClient.active) CmdSetLinksPositions(starts.ToArray(), ends.ToArray());
            else RpcSetLinksPositions(starts.ToArray(), ends.ToArray());
        }

        [Command]
        private void CmdSetLinksPositions(Vector3[] starts, Vector3[] ends)
        {
            RpcSetLinksPositions(starts, ends);
        }

        [ClientRpc]
        private void RpcSetLinksPositions(Vector3[] starts, Vector3[] ends)
        {
            for (int i = 0; i < starts.Length; i++)
            {
                var link = _shurikenLinks[i];
                link.startPos = starts[i];
                link.endPos = ends[i];
            }
        }

        public void AddProjectile(LinkedShurikenProjectile projectile)
        {
            projectile.onDestroy.AddListener(OnProjectileDestroy);
            _projectiles.Add(projectile);
            activeLinksCount = GetTargetLinksCount();
        }

        private void OnProjectileDestroy(LinkedShurikenProjectile projectile)
        {
            _projectiles.Remove(projectile);
            activeLinksCount = GetTargetLinksCount();
            if (_projectiles.Count == 0) NetworkServer.Destroy(gameObject);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetTargetLinksCount() => _projectiles.Count < 3 ? _projectiles.Count - 1 : _projectiles.Count;
    }
}