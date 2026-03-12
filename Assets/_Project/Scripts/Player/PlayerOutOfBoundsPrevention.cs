using System;
using System.Collections.Generic;
using Game.Core.Events;
using Game.Core.Lobby;
using Game.Core.Maps;
using Game.Player.Events;
using UnityEngine;

namespace Game.Player
{
    public class PlayerOutOfBoundsPrevention : MonoBehaviour
    {
        public LobbyInfo lobbyInfo;

        private Dictionary<Guid, PlayerBase> _players;
        private Stack<PlayerBase> _newPlayers;
        private Stack<Guid> _destroyedPlayers;

        private void Awake()
        {
            _players = new();
            _newPlayers = new();
            _destroyedPlayers = new();

            EventBus<OnPlayerInitialized>.Listen((data) => _newPlayers.Push(data.player));
            EventBus<OnPlayerDestroy>.Listen((data) => _destroyedPlayers.Push(data.guid));
        }

        private void Update()
        {
            while (_newPlayers.Count > 0)
            {
                var player = _newPlayers.Pop();
                _players.Add(player.Identification.guid, player);
            }

            while (_destroyedPlayers.Count > 0)
            {
                var guid = _destroyedPlayers.Pop();
                _players.Remove(guid);
            }

            foreach (var (_, player) in _players)
            {
                var playerCenter = player.motor.Capsule.bounds.center;
                if (MapLoader.IsPlayerOnMap(player) && !MapLoader.loadedMap.info.Bounds.Contains(playerCenter))
                {
                    var position = MapLoader.loadedMap.info.GetRandomSpawnPoint();
                    player.ServerMovePlayer(position);
                }
                else if (playerCenter.y < lobbyInfo.minBoundsHeight)
                {
                    player.ServerMovePlayer(lobbyInfo.spawnArea.RandomSampleArea(Space.World));
                }
            }
        }
    }
}