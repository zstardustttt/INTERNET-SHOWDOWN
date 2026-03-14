using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Events;
using Game.Core.Hits.Events;
using UnityEngine;

namespace Game.Core.Hits
{
    public class HitWatcher : MonoBehaviour
    {
        public const int QUERY_BUFFER_SIZE = 128;

        public float queryMargin;
        public HitLayer[] layers;

        private List<HitEntity> _entities;
        private Stack<HitEntity> _freshEntities;
        private List<int> _outdatedEntities;

        private Collider[] _queryCollidersBuffer;
        private RaycastHit[] _queryHitsBuffer;
        private LayerMask _queryMask;

        private void OnValidate()
        {
            for (int i = 0; i < layers.Length; i++)
            {
                layers[i].cachedIndex = i;
            }
        }

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

                var hitLayerMask = 0;
                foreach (var source in data.entity.sources)
                {
                    if (!source || !source.layer) continue;
                    hitLayerMask |= 1 << source.layer.cachedIndex;
                }

                foreach (var target in data.entity.targets)
                {
                    if (!target || !target.layer) continue;
                    hitLayerMask |= 1 << target.layer.cachedIndex;
                }
                data.entity.hitLayerMask = hitLayerMask;

                _freshEntities.Push(data.entity);
            });

            EventBus<OnHitEntityDestroy>.Listen((data) =>
            {
                if (data.index > -1)
                    _outdatedEntities.Add(data.index);
            });
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
            if (_outdatedEntities.Count == 0) return;

            var ordered = _outdatedEntities.OrderByDescending((x) => x);
            foreach (var index in ordered)
            {
                if (index < 0 || index >= _entities.Count)
                {
                    Debug.LogError($"Failed to remove outdated entity with index {index}! entities count: {_entities.Count}");
                    return;
                }

                _entities.RemoveAt(index);
            }
            _outdatedEntities.Clear();

            for (int i = 0; i < _entities.Count; i++)
            {
                _entities[i].index = i;
            }
        }

        private void AppendFreshEntities()
        {
            while (_freshEntities.Count > 0)
            {
                var entity = _freshEntities.Pop();
                if (!entity) continue;

                entity.index = _entities.Count;
                _entities.Add(entity);
            }
        }

        private void BeforeHitScan()
        {
            foreach (var entity in _entities)
            {
                if (!entity) continue;

                var sourcesActive = false;
                foreach (var source in entity.sources)
                {
                    source.BeforeHitScan();
                    source.beforeHitScan.Invoke();

                    source.UpdateActivity();
                    if (source.Active) sourcesActive = true;
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

                if (entity.skipObservationUpdate)
                {
                    entity.skipObservationUpdate = false;
                }
                else
                {
                    entity.observedPosition = entity.transform.position;
                    entity.observedDelta = entity.observedPosition - entity.previousObservedPosition;
                    entity.previousObservedPosition = entity.observedPosition;
                }
            }
        }

        private void HitScan()
        {
            foreach (var selfEntity in _entities)
            {
                if (!selfEntity.Active || !selfEntity.SourcesActive) continue;

                foreach (var otherEntity in _entities)
                {
                    if (!otherEntity.Active || !otherEntity.TargetsActive) continue;
                    if (selfEntity.index == otherEntity.index) continue;

                    // skip if sharing family
                    if (selfEntity.family != Guid.Empty && otherEntity.family != Guid.Empty && selfEntity.family == otherEntity.family) continue;

                    // Check for overlap in hit layer masks
                    if ((selfEntity.hitLayerMask & otherEntity.hitLayerMask) == 0) continue;

                    if (!EntityPairCheck(selfEntity, otherEntity, out var hitPoint)) continue;
                    InvokeHitEvents(selfEntity, otherEntity, hitPoint);
                }
            }
        }

        private void InvokeHitEvents(HitEntity self, HitEntity other, Vector3 point)
        {
            foreach (var source in self.sources)
            {
                if (!source.Active || !source.layer) continue;

                foreach (var target in other.targets)
                {
                    if (!target.Active || !target.layer) continue;
                    if (source.layer.cachedIndex != target.layer.cachedIndex) continue;

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
            var queryOrigin = self.observedPosition;

            var hitsCount = self.OverlapNonAlloc(queryOrigin, _queryMask, _queryCollidersBuffer, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hitsCount; i++)
            {
                var collider = _queryCollidersBuffer[i];
                if (collider == other.Collider)
                {
                    point = (self.observedPosition + other.observedPosition) / 2f;
                    return true;
                }
            }

            // Cast from current position backwards
            var relativeDelta = other.observedDelta - self.observedDelta;
            var queryDirection = relativeDelta.normalized;
            var queryLength = relativeDelta.magnitude + queryMargin;

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
                        self.observedPosition,
                        self.observedPosition - self.observedDelta,
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