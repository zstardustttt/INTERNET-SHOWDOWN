using UnityEngine;
using Mirror;
using UnityEngine.Events;
using Game.Core.Events;
using System.Linq;
using Game.Core.Player.Death.Events;
using Game.Core.Player.Locks;

namespace Game.Core.Player.Death
{
    public enum RespawnInvincibilityState
    {
        None,
        Awoken,
        Ending
    }

    [RequireComponent(typeof(PlayerCore))]
    public class PlayerDeathModule : NetworkBehaviour
    {
        public PlayerCore player;
        public PlayerDeathConfig config;
        public GameObject ghostModel;
        public MeshRenderer[] respawnEffectRenderers;

        [HideInInspector] public UnityEvent onDeath = new();
        [HideInInspector] public UnityEvent onRespawn = new();
        [HideInInspector] public float respawnTimer;
        [HideInInspector] public Vector3 previousPosition;

        public bool Dead => _dead;
        [SyncVar(hook = nameof(OnDeathOrRespawn))] private bool _dead;

        [SyncVar(hook = nameof(OnInvincibilityStateChanged))] private RespawnInvincibilityState _invincibilityState;
        private float _accumulatedMoveDistance;
        private float _awakeTimer;
        private float _endingTimer;

        private Material[][] _baseMaterials;
        private Material[][] _baseWithRespawnMaterials;
        private Material _respawnMaterial;

        private LayerMask _enviromentMask;

        protected override void OnValidate()
        {
            base.OnValidate();

            if (Application.isPlaying) return;
            player = GetComponent<PlayerCore>();
        }

        private void Start()
        {
            _enviromentMask = LayerMask.GetMask("Enviroment");
            if (!NetworkClient.active) return;

            _respawnMaterial = Instantiate(config.respawnEffectMaterial);
            _baseMaterials = new Material[respawnEffectRenderers.Length][];
            _baseWithRespawnMaterials = new Material[respawnEffectRenderers.Length][];
            for (int i = 0; i < respawnEffectRenderers.Length; i++)
            {
                var mats = respawnEffectRenderers[i].materials;
                _baseMaterials[i] = mats;

                var newMaterials = mats.ToList();
                newMaterials.Add(_respawnMaterial);
                _baseWithRespawnMaterials[i] = newMaterials.ToArray();
            }
        }

        private void OnDeathOrRespawn(bool old, bool _new)
        {
            if (_new) onDeath.Invoke();
            else onRespawn.Invoke();

            player.mainModel.SetActive(!_new);
            ghostModel.SetActive(_new);
        }

        private void OnInvincibilityStateChanged(RespawnInvincibilityState old, RespawnInvincibilityState _new)
        {
            EventBus<OnPlayerRespawnInvincibilityStateChanged>.Invoke(new()
            {
                player = player,
                state = _new
            });

            if (_new != RespawnInvincibilityState.None)
            {
                var interval = _new == RespawnInvincibilityState.Awoken ? 2f : 0.1f;
                _respawnMaterial.SetFloat("_Interval", interval);

                for (int i = 0; i < respawnEffectRenderers.Length; i++)
                {
                    respawnEffectRenderers[i].materials = _baseWithRespawnMaterials[i];
                }

                return;
            }

            for (int i = 0; i < respawnEffectRenderers.Length; i++)
            {
                respawnEffectRenderers[i].materials = _baseMaterials[i];
            }
        }

        private void Update()
        {
            // Ascend if dead
            if (player.HandlingThisPlayer && _dead)
            {
                var delta = config.ascendSpeed * Time.deltaTime;
                var distance = player.movementModule.motor.Capsule.height / 2f + delta;
                var isCeiled = Physics.Raycast(player.movementModule.motor.Capsule.bounds.center, Vector3.up, distance, _enviromentMask);

                if (!isCeiled) transform.position += delta * Vector3.up;
            }

            // Handle invincibility
            if (!NetworkServer.active || _invincibilityState == RespawnInvincibilityState.None) return;

            if (_invincibilityState == RespawnInvincibilityState.Awoken)
            {
                if (_awakeTimer > 0f)
                {
                    _awakeTimer -= Time.deltaTime;
                    return;
                }

                if (_accumulatedMoveDistance < config.moveThreshold)
                {
                    _accumulatedMoveDistance += Vector3.Distance(transform.position, previousPosition);
                    previousPosition = transform.position;
                    return;
                }

                player.locks.Unlock(PlayerLock.Force);
                _invincibilityState = RespawnInvincibilityState.Ending;
            }

            if (_invincibilityState == RespawnInvincibilityState.Ending)
            {
                if (_endingTimer > 0f)
                {
                    _endingTimer -= Time.deltaTime;
                    return;
                }

                player.locks.Unlock(PlayerLock.Health);
                _invincibilityState = RespawnInvincibilityState.None;
            }
        }

        public void Die()
        {
            if (_dead) return;
            _dead = true;

            player.itemModule.ResetItem();
            player.locks.Lock(PlayerLocks.all);
        }

        public void Respawn()
        {
            if (!_dead) return;
            _dead = false;

            player.locks.Unlock(PlayerLock.Hit, PlayerLock.Input, PlayerLock.Motor);
            player.healthModule.ResetHealth();

            _accumulatedMoveDistance = 0f;
            _invincibilityState = RespawnInvincibilityState.Awoken;
            _awakeTimer = config.awakeInvincibilityDuration;
            _endingTimer = config.endingInvincibilityDuration;
        }
    }
}