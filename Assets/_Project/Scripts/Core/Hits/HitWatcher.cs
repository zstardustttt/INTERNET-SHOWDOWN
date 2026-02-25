using System;
using System.Collections.Generic;
using Game.Core.Events;
using Game.Core.Hits.Events;
using UnityEngine;

namespace Game.Core.Hits
{
    public class HitWatcher : MonoBehaviour
    {
        public const int QUERY_BUFFER_SIZE = 128;

        public float queryMargin;

        private Dictionary<Guid, HitEntity> _entities;
        private Stack<HitEntity> _freshEntities;
        private Stack<Guid> _outdatedEntities;

        private Collider[] _queryCollidersBuffer;
        private RaycastHit[] _queryHitsBuffer;
        private LayerMask _queryMask;

        private void Awake()
        {
            _entities = new();
            _freshEntities = new();
            _outdatedEntities = new();

            _queryCollidersBuffer = new Collider[QUERY_BUFFER_SIZE];
            _queryHitsBuffer = new RaycastHit[QUERY_BUFFER_SIZE];
            _queryMask = LayerMask.GetMask("HitEntity");

            EventBus<OnHitEntityCreate>.Listen((data) =>
            {
                data.entity.guid = Guid.NewGuid();
                _freshEntities.Push(data.entity);
            });
            EventBus<OnHitEntityDestroy>.Listen((data) => _outdatedEntities.Push(data.guid));
        }

        private void Update()
        {
            CleanupOutdatedEntities();
            AppendFreshEntities();

            BeforeHitScan();
            HitScan();
        }

        private void CleanupOutdatedEntities()
        {
            while (_outdatedEntities.Count > 0)
            {
                var guid = _outdatedEntities.Pop();
                _entities.Remove(guid);
            }
        }

        private void AppendFreshEntities()
        {
            while (_freshEntities.Count > 0)
            {
                var entity = _freshEntities.Pop();
                if (!entity) continue;
                _entities.Add(entity.guid, entity);
            }
        }

        private void BeforeHitScan()
        {
            foreach (var (_, entity) in _entities)
            {
                if (!entity) continue;

                var sourcesActive = false;
                foreach (var source in entity.sources)
                {
                    source.BeforeHitScan();
                    source.beforeHitScan.Invoke();

                    source.UpdateActivity();
                    if (source.active) sourcesActive = true;
                }

                var targetsActive = false;
                foreach (var target in entity.targets)
                {
                    target.BeforeHitScan();
                    target.beforeHitScan.Invoke();

                    target.UpdateActivity();
                    if (target.Active) targetsActive = true;
                }

                entity.UpdateActivity(sourcesActive, targetsActive);

                entity.observedDelta = entity.transform.position - entity.previousObservedPosition;
                entity.previousObservedPosition = entity.transform.position;
                entity.completedSelfChecks = false;
            }
        }

        private void HitScan()
        {
            foreach (var (_, selfEntity) in _entities)
            {
                if (!selfEntity) continue;
                if (!selfEntity.Active || !selfEntity.SourcesActive) continue;

                // Setting to true immideatly so there is no need to compare self guid and other guid
                selfEntity.completedSelfChecks = true;

                foreach (var (_, otherEntity) in _entities)
                {
                    if (!otherEntity) continue;
                    if (!otherEntity.Active || !otherEntity.TargetsActive) continue;
                    if (otherEntity.completedSelfChecks) continue;

                    if (!EntityPairCheck(selfEntity, otherEntity, out var hitPoint)) continue;
                    InvokeHitEvents(selfEntity, otherEntity, hitPoint);
                    InvokeHitEvents(otherEntity, selfEntity, hitPoint); // Inversed
                }
            }
        }

        private void InvokeHitEvents(HitEntity self, HitEntity other, Vector3 point)
        {
            foreach (var source in self.sources)
            {
                if (!source.Active) continue;

                foreach (var target in other.targets)
                {
                    if (!target.Active) continue;

                    var hitEvent = new HitEvent(source, self, target, other, point);
                    source.onHit.Invoke(hitEvent);
                    self.onHit.Invoke(hitEvent);
                    target.onHit.Invoke(hitEvent);
                    other.onHit.Invoke(hitEvent);
                    EventBus<HitEvent>.Invoke(hitEvent);
                }
            }
        }

        public bool EntityPairCheck(HitEntity self, HitEntity other, out Vector3 point)
        {
            // Cast from current position backwards
            var relativeDelta = other.observedDelta - self.observedDelta;

            var queryOrigin = self.transform.position;
            var queryDirection = relativeDelta.normalized;
            var queryLength = relativeDelta.magnitude + queryMargin;

            var hitsCount = self.OverlapNonAlloc(queryOrigin, _queryMask, _queryCollidersBuffer, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hitsCount; i++)
            {
                var collider = _queryCollidersBuffer[i];
                if (collider == other.Collider)
                {
                    point = (self.transform.position + other.transform.position) / 2f;
                    return true;
                }
            }

            if (queryLength == queryMargin)
            {
                point = Vector3.zero;
                return false;
            }

            hitsCount = self.CastNonAlloc(queryOrigin, queryDirection, queryLength, _queryMask, _queryHitsBuffer, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hitsCount; i++)
            {
                var hit = _queryHitsBuffer[i];
                if (hit.collider == other.Collider)
                {
                    point = Vector3.Lerp
                    (
                        self.transform.position,
                        self.transform.position - self.observedDelta,
                        hit.distance / queryLength
                    );

                    return true;
                }
            }

            point = Vector3.zero;
            return false;
        }
    }
}