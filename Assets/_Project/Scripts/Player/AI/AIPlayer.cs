using UnityEngine;
using Mirror;
using Game.Core.Player;
using System;
using Random = UnityEngine.Random;
using Game.Core.Player.Movement;
using Game.Core.Maps;

namespace Game.Player.AI
{
    [RequireComponent(typeof(PlayerCore))]
    public class AIPlayer : NetworkBehaviour, IPlayerMovementController
    {
        public PlayerCore player;

        private GameObject _portal;

        protected override void OnValidate()
        {
            base.OnValidate();

            if (Application.isPlaying) return;
            player = GetComponent<PlayerCore>();
        }

        public override void OnStartServer()
        {
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

        public PlayerMovementInputs GetInputs()
        {
            if (player.State == PlayerState.InLobby)
                return GetLobbyInputs();

            if (player.State == PlayerState.InMatch)
                return GetMatchInputs();

            return new();
        }

        private PlayerMovementInputs GetLobbyInputs()
        {
            if (!_portal)
            {
                _portal = GameObject.FindGameObjectWithTag("Portal");
                if (!_portal) return new();
            }

            LookAt(_portal.transform.position);
            return new()
            {
                move = new(0f, 1f)
            };
        }

        private PlayerMovementInputs GetMatchInputs()
        {
            return new();
        }

        private void LookAt(Vector3 position)
        {
            var dir = (position - player.verticalOrientation.position).normalized;
            player.horizontalOrientation.localEulerAngles = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg * Vector3.up;
            player.verticalOrientation.localEulerAngles = Mathf.Asin(dir.y) * Mathf.Rad2Deg * Vector3.right;
        }
    }
}