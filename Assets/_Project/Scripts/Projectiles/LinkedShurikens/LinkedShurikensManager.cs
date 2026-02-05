using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Game.Core.Damage;
using Game.Core.Events;
using Game.Events.HitWatcher;
using Mirror;
using UnityEngine;

namespace Game.Projectiles.LinkedShurikens
{
    public class LinkedShurikensManager : NetworkBehaviour
    {
        public ShurikenLink shurikenLinkPrefab;
        public DamageDealer hitDealer;
        public int maxLinksCount;
        public int linkSegmentsCount;
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
            _shurikenLinks = new();
            for (int i = 0; i < maxLinksCount; i++)
            {
                var link = Instantiate(shurikenLinkPrefab.gameObject, transform).GetComponent<ShurikenLink>();
                link.lineRenderer.positionCount = linkSegmentsCount + 2;
                _shurikenLinks.Add(link);
                link.gameObject.SetActive(false);
            }

            if (!NetworkServer.active) return;
            _projectiles = new();
        }

        private void Update()
        {
            var localPlayerTransform = NetworkClient.localPlayer.transform;
            foreach (var link in _shurikenLinks)
            {
                if (!localPlayerTransform) break;
                if (!link.gameObject.activeInHierarchy) continue;

                // Electricity effect
                var start = link.lineRenderer.GetPosition(0);
                var end = link.lineRenderer.GetPosition(linkSegmentsCount + 1);
                for (int i = 1; i <= linkSegmentsCount; i++)
                {
                    Vector3 pointOnLine = Vector3.Lerp(start, end, (float)i / (linkSegmentsCount + 1));
                    Vector3 offset = Random.insideUnitSphere * 0.5f;
                    link.lineRenderer.SetPosition(i, pointOnLine + offset);
                }

                // Move audio source
                var middlePoint = (end + start) / 2f;
                var lineDirection = (end - start).normalized;
                var targetDirection = localPlayerTransform.position - middlePoint;
                var projection = Vector3.ClampMagnitude(Vector3.Project(targetDirection, lineDirection), (end - start).magnitude / 2f);
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

                if (projectile != previousProjectile)
                {
                    EventBus<RequestTwoPointsDealerCheck>.Invoke(new()
                    {
                        dealer = hitDealer,
                        point1 = start,
                        point2 = end,
                        ignoreInactive = true,
                    });
                }

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
                link.lineRenderer.SetPosition(0, starts[i]);
                link.lineRenderer.SetPosition(linkSegmentsCount + 1, ends[i]);
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