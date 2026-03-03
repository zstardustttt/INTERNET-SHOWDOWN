using UnityEngine;
using Mirror;
using UnityEngine.Events;

namespace Game.Player.Death
{
    public class PlayerDeathModule : NetworkBehaviour
    {
        [Header("Objects")]
        public PlayerBase player;
        public PlayerDeathConfig config;
        public GameObject ghostModel;

        [HideInInspector] public UnityEvent onDeath = new();
        [HideInInspector] public UnityEvent onRespawn = new();
        [HideInInspector] public float respawnTimer;

        public bool Dead => _dead;
        [SyncVar(hook = nameof(OnDeathOrRespawn))] private bool _dead;

        private void OnDeathOrRespawn(bool old, bool _new)
        {
            if (_new) onDeath.Invoke();
            else onRespawn.Invoke();

            player.mainModel.SetActive(!_new);
            ghostModel.SetActive(_new);
        }

        private void Update()
        {
            // Ascend if dead
            if (player.HandlingThisPlayer && _dead)
            {
                var delta = config.ascendSpeed * Time.deltaTime;
                var distance = player.motor.Capsule.height / 2f + delta;
                var isCeiled = Physics.Raycast(player.motor.Capsule.bounds.center, Vector3.up, distance, LayerMask.GetMask("Enviroment"));

                if (!isCeiled) transform.position += delta * Vector3.up;
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
            player.locks.Unlock(PlayerLocks.all);
            player.healthModule.ResetHealth();
        }
    }
}