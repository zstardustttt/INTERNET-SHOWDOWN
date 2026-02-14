using System.Collections.Generic;
using System.Linq;
using Game.Core.Events;
using Game.Core.Maps;
using Game.Events.GameLoop;
using Game.Events.MapLoader;
using Game.Events.MusicPlayer;
using Game.Events.Player;
using Game.Events.UI;
using Game.Systems;
using Game.Network.Messages;
using Game.Player;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

using Random = UnityEngine.Random;

namespace Game.Network
{
    public class CustomNetworkManager : NetworkManager
    {
        public TransitionManager transitionManager;
        public static CustomNetworkManager CustomSingleton => (CustomNetworkManager)singleton;
        private GameObject _portal;

        private Dictionary<string, PlayerStats> _disconnectedPlayersStats;
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
                var position = MapLoader.loadedMap.info.spawnPoints[Random.Range(0, MapLoader.loadedMap.info.spawnPoints.Length)].position;
                conn.identity.GetComponent<PlayerBase>().ServerMovePlayer(position);
                conn.Send<ServerConfirmPlayerEnteredMatch>(new());
            });

            EventBus<OnGameStateChange>.Listen((data) =>
            {
                if (data.state.phase.type == GamePhaseType.Preparation)
                    _disconnectedPlayersStats.Clear();

                _gameState = data.state;
            });

            EventBus<OnAddPlayerOnMap>.Listen((data) =>
            {
                if (_gameState.phase.type == GamePhaseType.Match) data.player.PickRandomItem();

                NetworkServer.SendToAll(new ServerAddLeaderboardItem()
                {
                    itemData = new()
                    {
                        guid = data.player.playerGuid,
                        item = new()
                        {
                            name = data.player.playerName,
                            activity = data.player.stats.activity,
                            score = data.player.stats.GetScore()
                        }
                    }
                });
            });

            EventBus<OnDestroyPlayer>.Listen((data) =>
            {
                if (!NetworkServer.active) return;

                NetworkServer.SendToAll(new ServerRemoveLeaderboardItem()
                {
                    guid = data.guid
                });
            });

            EventBus<OnStatsChanged>.Listen((data) =>
            {
                NetworkServer.SendToAll(new ServerChangeLeaderboardItem()
                {
                    itemData = new()
                    {
                        guid = data.player.playerGuid,
                        item = new()
                        {
                            name = data.player.playerName,
                            activity = data.player.stats.activity,
                            score = data.player.stats.GetScore()
                        }
                    }
                });
            });

            EventBus<OnUnloadMap>.Listen((_) =>
            {
                NetworkServer.SendToAll(new ServerClearLeaderboard());
            });

            EventBus<OnServerOnlinePlayerInitialized>.Listen((data) =>
            {
                if (!_gameState.phase.info.loadStats) return;

                if (_disconnectedPlayersStats.TryGetValue(data.player.playerGuid, out var stats))
                {
                    data.player.stats = stats;
                    _disconnectedPlayersStats.Remove(data.player.playerGuid);
                }
            });
        }

        public override void OnStopServer()
        {
            MapLoader.Stop();
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            var player = conn.identity.GetComponent<PlayerBase>();
            if (_gameState.phase.info.saveStats)
            {
                _disconnectedPlayersStats.Add(player.playerGuid, player.stats);
            }

            // base destroys connection's player
            base.OnServerDisconnect(conn);
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
                            name = x.Value.playerName,
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

            NetworkClient.RegisterHandler<ServerConfirmPlayerEnteredMatch>((data) =>
            {
                EventBus<RequestMatchMusic>.Invoke(new());
                EventBus<RequestGameplayUI>.Invoke(new());
            });

            EventBus<OnGameStateChange>.Listen((data) =>
            {
                if (data.state.phase.type == GamePhaseType.Break) EventBus<StopMatchMusic>.Invoke(new());

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