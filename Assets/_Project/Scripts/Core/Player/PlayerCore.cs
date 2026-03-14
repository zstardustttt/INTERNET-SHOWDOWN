using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Damages;
using Game.Core.Events;
using Game.Core.Hits;
using Game.Core.Player.Death;
using Game.Core.Player.Events;
using Game.Core.Player.Health;
using Game.Core.Player.Items;
using Game.Core.Player.Locks;
using Game.Core.Player.Movement;
using Game.Core.Player.Stats;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Core.Player
{
    public struct PlayerIdentification
    {
        public string name;
        public Guid guid;
    }

    [RequireComponent(typeof(PlayerMovementModule), typeof(PlayerItemModule))]
    [RequireComponent(typeof(PlayerHealthModule), typeof(PlayerDeathModule))]
    [RequireComponent(typeof(PlayerLocks), typeof(TeamReference))]
    public sealed class PlayerCore : NetworkBehaviour
    {
        public bool Initialized { get; private set; }
        public bool HandlingThisPlayer { get; private set; }

        public PlayerConfig config;
        public CapsuleHitEntity hitEntity;
        public PlayerMovementModule movementModule;
        public PlayerItemModule itemModule;
        public PlayerHealthModule healthModule;
        public PlayerDeathModule deathModule;
        public PlayerLocks locks;
        public TeamReference teamReference;

        [Space(9)]
        public Transform horizontalOrientation;
        public Transform verticalOrientation;

        [Space(9)]
        public GameObject modelContainer;
        public GameObject mainModel;

        [SyncVar] public PlayerStats stats;
        private PlayerStats _previousStats;

        public PlayerIdentification Identification => _identification;
        [SyncVar] private PlayerIdentification _identification;

        [HideInInspector] public UnityEvent onHandlingThisPlayer;
        [HideInInspector] public UnityEvent<PlayerIdentification, DamageIdentification, DamageType, float> onDealtDamage;
        [HideInInspector] public UnityEvent<Collider> onLocalTriggerEnter;

        private Collider[] _triggerBuffer;
        private Collider[] _previousTriggerBuffer;
        private int _previousTriggerOverlapsCount;

        protected override void OnValidate()
        {
            base.OnValidate();

            if (!Application.isPlaying)
            {
                movementModule = GetComponent<PlayerMovementModule>();
                itemModule = GetComponent<PlayerItemModule>();
                healthModule = GetComponent<PlayerHealthModule>();
                deathModule = GetComponent<PlayerDeathModule>();
                locks = GetComponent<PlayerLocks>();
                teamReference = GetComponent<TeamReference>();
            }

            if (!hitEntity)
                throw new("Hit Entity on player isn't assigned!");

            hitEntity.capsuleCollider.radius = config.hitCapsuleRadius;
            hitEntity.capsuleCollider.center = Vector3.up * config.hitCapsuleOffset;
            hitEntity.capsuleCollider.height = config.hitCapsuleHeight;
        }

        public void HandleThisPlayer(PlayerIdentification identification)
        {
            onHandlingThisPlayer.Invoke();
            HandlingThisPlayer = true;

            if (NetworkServer.active) Initialize(identification);
            else CmdInitialize(identification);
        }

        [Command]
        private void CmdInitialize(PlayerIdentification identification) => Initialize(identification);

        [Server]
        private void Initialize(PlayerIdentification identification)
        {
            _triggerBuffer = new Collider[config.triggerBufferCapacity];
            _previousTriggerBuffer = new Collider[config.triggerBufferCapacity];

            if (Initialized)
            {
                Debug.LogError($"Player {_identification.name} is already initialized!");
                return;
            }

            _identification = identification;
            Initialized = true;
            EventBus<OnPlayerInitialized>.Invoke(new() { player = this });
        }

        public override void OnStartServer()
        {
            locks.onLockStateChange.AddListener((plock, locked) =>
            {
                if (plock == PlayerLock.Hit) hitEntity.active = !locked;
            });

            teamReference.team = Guid.NewGuid();
        }

        private void Update()
        {
            if (!stats.Equals(_previousStats))
            {
                EventBus<OnPlayerStatsChanged>.Invoke(new()
                {
                    player = this,
                    current = stats,
                    previous = _previousStats
                });
            }

            _previousStats = stats;

            if (!HandlingThisPlayer) return;
            LocalTriggerCheck();
        }

        private void LocalTriggerCheck()
        {
            var point0 = transform.position + Vector3.up * config.hitCapsuleRadius;
            var point1 = transform.position + Vector3.up * (config.hitCapsuleHeight - config.hitCapsuleRadius);
            var count = Physics.OverlapCapsuleNonAlloc(point0, point1, config.hitCapsuleRadius, _triggerBuffer, config.localTriggerLayerMask, QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                var collider = _triggerBuffer[i];

                var enter = true;
                for (int j = 0; j < _previousTriggerOverlapsCount; j++)
                {
                    if (collider != _previousTriggerBuffer[j]) continue;

                    enter = false;
                    break;
                }

                if (enter) onLocalTriggerEnter.Invoke(collider);
            }

            Array.Copy(_triggerBuffer, _previousTriggerBuffer, count);
            _previousTriggerOverlapsCount = count;
        }

        [Server]
        public void ReportDealtDamage(PlayerCore target, Damage damage, float finalAmount)
        {
            stats.damageDealt += finalAmount;
            if (damage.type == DamageType.Direct) stats.directHits++;
            else if (damage.type == DamageType.Indirect) stats.indirectHits++;
            else throw new($"Damage type {damage.type} isn't supported");

            if (HandlingThisPlayer) onDealtDamage.Invoke(target._identification, damage.identification, damage.type, finalAmount);
            else TargetReportDealtDamage(target._identification, damage.identification, damage.type, finalAmount);
        }

        [TargetRpc]
        private void TargetReportDealtDamage(PlayerIdentification target, DamageIdentification source, DamageType type, float amount)
        {
            onDealtDamage.Invoke(target, source, type, amount);
        }

        [Server]
        public void ReportKill(KillType type)
        {
            if (type == KillType.Pure) stats.pureKills++;
            else if (type == KillType.Finishing) stats.finishingKills++;
            else if (type == KillType.Supporting) stats.supportingKills++;
            else throw new($"Kill type {type} isn't supported");
        }
    }
}