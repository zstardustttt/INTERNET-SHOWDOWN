using System.Collections.Generic;
using System.Linq;
using Game.Core.Events;
using Game.Core.Maps;
using Game.Events.UI;
using Game.Network.Messages;
using Game.Player;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

using Game.Player.Events;
using Game.GameLoop;
using Game.GameLoop.Events;
using Game.Maps.Events;
using Game.Core.Lobby;
using System;

namespace Game.Network
{
    public class CustomNetworkManager : NetworkManager
    {
        public TransitionManager transitionManager;
        public static CustomNetworkManager CustomSingleton => (CustomNetworkManager)singleton;
        private GameObject _portal;

        private Dictionary<Guid, PlayerStats> _disconnectedPlayersStats;
        private GameState _gameState;

        public override void OnStartServer()
        {
            _disconnectedPlayersStats = new();

            MapLoader.Init();
            NetworkServer.RegisterHandler<ClientRequestMapLoad>((conn, _) =>
            {
                if (!MapLoader.TryMoveGameObjectToMap(conn.identity.gameObject))
                {
                    Debug.LogWarning($"Client {conn.connectionId} wanted to load into unloaded map or has already loaded that map");
                    return;
                }

                var sceneName = MapLoader.loadedMap.config.sceneName;
                conn.Send(new SceneMessage() { sceneName = sceneName, sceneOperation = SceneOperation.LoadAdditive });
                conn.identity.GetComponent<PlayerBase>().ServerMovePlayer(MapLoader.loadedMap.info.GetRandomSpawnPoint());
                conn.Send<ServerOnlinePlayerAddedToMap>(new());
            });

            EventBus<OnGameStateChange>.Listen((data) =>
            {
                if (data.state.phase.type == GamePhaseType.Preparation)
                    _disconnectedPlayersStats.Clear();

                _gameState = data.state;
            });

            EventBus<OnAddPlayerToMap>.Listen((data) =>
            {
                if (_gameState.phase.type == GamePhaseType.Match)
                {
                    data.player.itemModule.PickRandomItem();
                }

                NetworkServer.SendToAll(new ServerAddLeaderboardItem()
                {
                    itemData = new()
                    {
                        guid = data.player.Identification.guid,
                        item = new()
                        {
                            name = data.player.Identification.name,
                            activity = data.player.stats.activity,
                            score = data.player.stats.GetScore()
                        }
                    }
                });
            });

            EventBus<OnPlayerDestroy>.Listen((data) =>
            {
                if (!NetworkServer.active) return;

                NetworkServer.SendToAll(new ServerRemoveLeaderboardItem()
                {
                    guid = data.guid
                });
            });

            EventBus<OnPlayerStatsChanged>.Listen((data) =>
            {
                var currentScore = data.current.GetScore();
                if (data.previous.activity == data.current.activity && data.previous.GetScore() == currentScore) return;

                NetworkServer.SendToAll(new ServerChangeLeaderboardItem()
                {
                    itemData = new()
                    {
                        guid = data.player.Identification.guid,
                        item = new()
                        {
                            name = data.player.Identification.name,
                            activity = data.current.activity,
                            score = currentScore
                        }
                    }
                });
            });

            EventBus<OnUnloadMap>.Listen((_) =>
            {
                NetworkServer.SendToAll(new ServerClearLeaderboard());
                NetworkServer.SendToAll(new ServerOnlinePlayerRemovedFromMap());
            });

            EventBus<OnPlayerInitialized>.Listen((data) =>
            {
                if (!_gameState.phase.info.loadStats) return;

                if (_disconnectedPlayersStats.TryGetValue(data.player.Identification.guid, out var stats))
                {
                    data.player.stats = stats;
                    _disconnectedPlayersStats.Remove(data.player.Identification.guid);
                }
            });
        }

        public override void OnStopServer()
        {
            MapLoader.Stop();
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            if (conn != null && conn.identity)
            {
                var player = conn.identity.GetComponent<PlayerBase>();
                if (_gameState.phase.info.saveStats)
                {
                    _disconnectedPlayersStats.Add(player.Identification.guid, player.stats);
                }
            }

            // base destroys connection's player
            base.OnServerDisconnect(conn);
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            var position = LobbyInfo.Singleton.spawnArea.RandomSampleArea(Space.World);
            var player = Instantiate(playerPrefab, position, Quaternion.identity);
            player.name = $"{playerPrefab.name} [connId={conn.connectionId}]";
            NetworkServer.AddPlayerForConnection(conn, player);
        }

        public override void OnServerReady(NetworkConnectionToClient conn)
        {
            base.OnServerReady(conn);

            if (MapLoader.loadedMap == null) return;
            conn.Send(new ServerPopulateLeaderboard()
            {
                itemDatas = MapLoader.loadedMap.players.Select(x =>
                {
                    return new LeaderboardEventData()
                    {
                        guid = x.Key,
                        item = new()
                        {
                            name = x.Value.Identification.name,
                            activity = x.Value.stats.activity,
                            score = x.Value.stats.GetScore()
                        }
                    };
                }).ToArray()
            });
        }

        public override void OnClientDisconnect()
        {
            Cursor.lockState = CursorLockMode.None;
        }

        public override void OnStartClient()
        {
            NetworkClient.RegisterHandler<ServerClearLeaderboard>((data) =>
            {
                EventBus<ClearLeaderboard>.Invoke(new());
            });

            NetworkClient.RegisterHandler<ServerPopulateLeaderboard>((data) =>
            {
                EventBus<PopulateLeaderboard>.Invoke(new() { itemDatas = data.itemDatas });
            });

            NetworkClient.RegisterHandler<ServerAddLeaderboardItem>((data) =>
            {
                EventBus<AddLeaderboardItem>.Invoke(new() { itemData = data.itemData });
            });

            NetworkClient.RegisterHandler<ServerRemoveLeaderboardItem>((data) =>
            {
                EventBus<RemoveLeaderboardItem>.Invoke(new() { guid = data.guid });
            });

            NetworkClient.RegisterHandler<ServerChangeLeaderboardItem>((data) =>
            {
                EventBus<ChangeLeaderboardItem>.Invoke(new() { itemData = data.itemData });
            });

            NetworkClient.RegisterHandler<ServerOnlinePlayerAddedToMap>((data) =>
            {
                EventBus<OnLocalPlayerAddedToMap>.Invoke(new());
                EventBus<RequestGameplayUI>.Invoke(new());
            });

            NetworkClient.RegisterHandler<ServerOnlinePlayerRemovedFromMap>((data) =>
            {
                EventBus<OnLocalPlayerRemovedFromMap>.Invoke(new());
            });

            EventBus<OnGameStateChange>.Listen((data) =>
            {
                if (!_portal) _portal = GameObject.FindGameObjectWithTag("Portal");
                _portal.SetActive(data.state.phase.info.activatePortal);
                // mirror for some reason automaticly disables mesh renderer
                _portal.GetComponent<MeshRenderer>().enabled = data.state.phase.info.activatePortal;
            });

            // only for pure clients
            if (!NetworkServer.active)
            {
                SceneManager.sceneLoaded += ClientSceneEnviromentApply;
                SceneManager.sceneUnloaded += ClientLobbyEnviromentApply;
            }
        }

        public override void OnStopClient()
        {
            // only for pure clients
            if (!NetworkServer.active)
            {
                SceneManager.sceneLoaded -= ClientSceneEnviromentApply;
                SceneManager.sceneUnloaded -= ClientLobbyEnviromentApply;
            }
        }

        [Client]
        private void ClientSceneEnviromentApply(Scene scene, LoadSceneMode mode)
        {
            SceneEnviromentData.TryApplyOnScene(scene);
        }

        [Client]
        private void ClientLobbyEnviromentApply(Scene scene)
        {
            SceneEnviromentData.TryApplyOnScene(SceneManager.GetActiveScene());
        }
    }
}