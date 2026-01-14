using System.Collections.Generic;
using System.Linq;
using Game.Core.Events;
using Game.Core.Maps;
using Game.Events.GameLoop;
using Game.Events.HitWatcher;
using Game.Events.MapLoader;
using Game.Events.MusicPlayer;
using Game.Events.UI;
using Game.Gameplay;
using Game.Network.Messages;
using Game.Player;
using Mirror;
using UnityEngine;

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

                conn.Send(new SceneMessage() { sceneName = MapLoader.loadedMap.config.sceneName, sceneOperation = SceneOperation.LoadAdditive });
                var position = MapLoader.loadedMap.info.spawnPoints[Random.Range(0, MapLoader.loadedMap.info.spawnPoints.Length)].position;
                conn.Send<ServerMovePlayer>(new() { position = position });
                conn.Send<ServerConfirmPlayerEnteredMatch>(new());
            });

            EventBus<OnPlayersOnMapUpdated>.Listen((_) =>
            {
                SendUpdateLeaderboard(MapLoader.loadedMap.players);
            });

            EventBus<OnHitsRegisteredThisFrame>.Listen((_) =>
            {
                SendUpdateLeaderboard(MapLoader.loadedMap.players);
            });
        }

        [Server]
        private void SendUpdateLeaderboard(List<PlayerBase> players)
        {
            NetworkServer.SendToAll(new ServerUpdateLeaderboard()
            {
                leaderboardItems = players.Select(x => new AddToLeaderboard()
                {
                    name = x.playerName,
                    directHits = x.directHits,
                    indirectHits = x.indirectHits
                }).ToArray()
            });
        }

        public override void OnClientDisconnect()
        {
            Cursor.lockState = CursorLockMode.None;
        }

        public override void OnStopServer()
        {
            MapLoader.Stop();
        }

        public override void OnStartClient()
        {
            NetworkClient.RegisterHandler<ServerMovePlayer>((data) =>
            {
                NetworkClient.localPlayer.GetComponent<PlayerBase>().SetPosition(data.position);
            });

            NetworkClient.RegisterHandler<ServerConfirmPlayerEnteredMatch>((data) =>
            {
                EventBus<RequestMatchMusic>.Invoke(new());
                EventBus<RequestGameplayUI>.Invoke(new());
            });

            NetworkClient.RegisterHandler<ServerUpdateLeaderboard>((data) =>
            {
                EventBus<ClearLeaderboard>.Invoke(new());
                foreach (var item in data.leaderboardItems)
                {
                    EventBus<AddToLeaderboard>.Invoke(item);
                }
            });

            EventBus<OnGameStateChange>.Listen((data) =>
            {
                if (data.state.phase == GamePhase.Break) EventBus<StopMatchMusic>.Invoke(new());

                if (!_portal) _portal = GameObject.FindGameObjectWithTag("Portal");
                _portal.SetActive(data.state.phase != GamePhase.Break);
                // mirror for some reason automaticly disables mesh renderer
                _portal.GetComponent<MeshRenderer>().enabled = data.state.phase != GamePhase.Break;
            });
        }
    }
}