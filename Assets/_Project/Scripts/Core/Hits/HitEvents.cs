using System;
using Game.Core.Events;
using UnityEngine;

namespace Game.Core.Hits.Events
{
    public struct OnHitEntityCreate : IEvent
    {
        public HitEntity entity;
    }

    public struct OnHitEntityDestroy : IEvent
    {
        public Guid guid;
    }

    public struct HitEvent : IEvent
    {
        public HitListener source;
        public HitEntity sourceEntity;

        public HitListener target;
        public HitEntity targetEntity;

        public Vector3 point;

        public HitEvent(HitListener source, HitEntity sourceEntity, HitListener target, HitEntity targetEntity, Vector3 point)
        {
            this.source = source;
            this.sourceEntity = sourceEntity;

            this.target = target;
            this.targetEntity = targetEntity;

            this.point = point;
        }
    }
}