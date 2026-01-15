using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Events;
using Game.Core.Maps;
using Game.Events.GameLoop;
using Game.Events.MapLoader;
using Game.Events.MusicPlayer;
using Game.Events.Player;
using Game.Events.UI;
using Game.Gameplay;
using Game.Network.Messages;
using Game.Player;
using Game.UI.Game;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

using Random = UnityEngine.Random;

namespace Game.Network
{
    public class CustomNetworkManager : NetworkManager
    {
        public static CustomNetworkManager CustomSingleton => (CustomNetworkManager)singleton;
        private GameObject _portal;

        public override void OnStartServer()
        {
            MapLoader.Init();
            NetworkServer.RegisterHandler<ClientRequestMapLoad>((conn, _) =>
            {
                if (!MapLoader.TryMoveGameObjectToMap(conn.identity.gameObject))
                {
                    Debug.LogWarning($"Client {conn.connectionId} wanted to load into unloaded map");
                    return;
                }

                var sceneName = MapLoader.loadedMap.config.sceneName;
                conn.Send(new SceneMessage() { sceneName = sceneName, sceneOperation = SceneOperation.LoadAdditive });
                var position = MapLoader.loadedMap.info.spawnPoints[Random.Range(0, MapLoader.loadedMap.info.spawnPoints.Length)].position;
                conn.identity.GetComponent<PlayerBase>().ServerMovePlayer(position);
                conn.Send<ServerConfirmPlayerEnteredMatch>(new());
            });

            EventBus<OnPlayersOnMapUpdated>.Listen((_) =>
            {
                SendRefreshLeaderboard(MapLoader.loadedMap.players);
            });

            EventBus<OnStatsChanged>.Listen((data) =>
            {
                SendUpdateLeaderboardItem(data.player);
            });
        }

        public override void OnStopServer()
        {
            MapLoader.Stop();
        }

        [Server]
        private void SendRefreshLeaderboard(List<PlayerBase> players)
        {
            NetworkServer.SendToAll(new ServerRefreshLeaderboard()
            {
                items = players.Select(player => new GuidItemPair()
                {
                    guid = player.playerGuid,
                    item = new()
                    {
                        playerName = player.playerName,
                        activity = player.stats.activity,
                        score = player.stats.GetScore()
                    }
                }).ToArray()
            });
        }

        [Server]
        private void SendUpdateLeaderboardItem(PlayerBase player)
        {
            NetworkServer.SendToAll<ServerUpdatePlayerOnLeaderboard>(new()
            {
                item = new()
                {
                    guid = player.playerGuid,
                    item = new()
                    {
                        playerName = player.playerName,
                        activity = player.stats.activity,
                        score = player.stats.GetScore(),
                    }
                }
            });
        }

        public override void OnClientDisconnect()
        {
            Cursor.lockState = CursorLockMode.None;
        }

        public override void OnStartClient()
        {
            NetworkClient.RegisterHandler<ServerConfirmPlayerEnteredMatch>((data) =>
            {
                EventBus<RequestMatchMusic>.Invoke(new());
                EventBus<RequestGameplayUI>.Invoke(new());
            });

            NetworkClient.RegisterHandler<ServerRefreshLeaderboard>((data) =>
            {
                EventBus<ClearLeaderboard>.Invoke(new());
                EventBus<PopulateLeaderboard>.Invoke(new() { items = data.items });
            });

            NetworkClient.RegisterHandler<ServerUpdatePlayerOnLeaderboard>((data) =>
            {
                EventBus<ChangeLeaderboardItem>.Invoke(data.item);
            });

            EventBus<OnGameStateChange>.Listen((data) =>
            {
                if (data.state.phase == GamePhase.Break) EventBus<StopMatchMusic>.Invoke(new());

                if (!_portal) _portal = GameObject.FindGameObjectWithTag("Portal");
                _portal.SetActive(data.state.phase != GamePhase.Break);
                // mirror for some reason automaticly disables mesh renderer
                _portal.GetComponent<MeshRenderer>().enabled = data.state.phase != GamePhase.Break;
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