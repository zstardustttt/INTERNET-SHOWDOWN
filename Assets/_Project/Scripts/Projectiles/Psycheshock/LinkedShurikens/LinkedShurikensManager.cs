using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Game.Core.Damages;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Projectiles.Psycheshock.LinkedShurikens
{
    public class LinkedShurikensManager : NetworkBehaviour
    {
        [Header("Objects")]
        public ShurikenLink shurikenLinkPrefab;
        public AuthorReference authorReference;
        public TeamReference teamReference;

        [Header("Properties")]
        public float pointsPerUnit;
        public int maxLinksCount;

        private List<LinkedShurikenProjectile> _projectiles;
        private List<LinkedShurikenProjectile> _collidedProjectiles;
        private List<ShurikenLink> _shurikenLinks;

        [SyncVar(hook = nameof(OnLinksPointsChanged))] private Vector3[] _linksPoints;

        private void OnLinksPointsChanged(Vector3[] old, Vector3[] _new)
        {
            var previousPoint = _new[^1];
            var linksCount = GetLinksCount(_new.Length);
            for (int i = 0; i < _shurikenLinks.Count; i++)
            {
                var link = _shurikenLinks[i];
                var active = i < linksCount;
                link.gameObject.SetActive(active);

                if (!active) break;
                var currentPoint = _new[i];

                link.startPos = previousPoint;
                link.endPos = currentPoint;

                var projectileDistance = Vector3.Distance(previousPoint, currentPoint);
                var pointsCount = Mathf.Max(Mathf.CeilToInt(projectileDistance * pointsPerUnit), 2);
                link.lineRenderer.positionCount = pointsCount;
                link.lineRendererPointsCount = pointsCount;

                link.lineRenderer.SetPosition(0, previousPoint);
                link.lineRenderer.SetPosition(pointsCount - 1, currentPoint);

                previousPoint = currentPoint;
            }
        }

        private void Awake()
        {
            _projectiles = new();
            _collidedProjectiles = new();
            _shurikenLinks = new();

            for (int i = 0; i < maxLinksCount; i++)
            {
                var link = Instantiate(shurikenLinkPrefab.gameObject, transform).GetComponent<ShurikenLink>();
                link.lineRenderer.positionCount = 2;
                _shurikenLinks.Add(link);

                link.gameObject.SetActive(false);
            }
        }

        public override void OnStartServer()
        {
            foreach (var link in _shurikenLinks)
            {
                link.damageSource.authorReference = authorReference;
                link.damageSource.teamReference = teamReference;
                link.hitEntity.active = false;
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
                for (int i = 1; i < link.lineRendererPointsCount - 1; i++)
                {
                    var pointOnLine = Vector3.Lerp(link.startPos, link.endPos, (float)i / link.lineRendererPointsCount);
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
        }

        public void AddProjectile(LinkedShurikenProjectile projectile)
        {
            projectile.onCollide.AddListener(() =>
            {
                projectile.onDestroy.AddListener(() =>
                {
                    _collidedProjectiles.Remove(projectile);
                    UpdateLinksPoints();
                });

                _collidedProjectiles.Add(projectile);
                UpdateLinksPoints();
            });

            projectile.onDestroy.AddListener(() =>
            {
                _projectiles.Remove(projectile);
                if (_projectiles.Count == 0) NetworkServer.Destroy(gameObject);
            });

            _projectiles.Add(projectile);
        }

        private void UpdateLinksPoints()
        {
            if (_collidedProjectiles.Count == 0) return;
            _linksPoints = _collidedProjectiles.Select(x => x.transform.position).ToArray();

            var previousPoint = _linksPoints[^1];
            var linksCount = GetLinksCount(_linksPoints.Length);
            for (int i = 0; i < _shurikenLinks.Count; i++)
            {
                var link = _shurikenLinks[i];
                var active = i < linksCount;
                link.gameObject.SetActive(active);

                if (!active) break;
                var currentPoint = _linksPoints[i];

                link.hitEntity.transform.position = (previousPoint + currentPoint) / 2f;
                link.hitEntity.transform.up = (previousPoint - currentPoint).normalized;
                link.hitEntity.capsuleCollider.height = (previousPoint - currentPoint).magnitude;
                link.hitEntity.active = true;
                previousPoint = currentPoint;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetLinksCount(int pointsCount) => pointsCount <= 2 ? pointsCount - 1 : pointsCount;
    }
}