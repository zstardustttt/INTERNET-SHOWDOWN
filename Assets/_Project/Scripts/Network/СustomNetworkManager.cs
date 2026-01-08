using Game.Core.Events;
using Game.Core.Maps;
using Game.Events.GameLoop;
using Game.Events.MusicPlayer;
using Game.Events.UI;
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

            EventBus<OnGameStateChange>.Listen((data) =>
            {
                if (data.state.phase == Gameplay.GamePhase.Break) EventBus<StopMatchMusic>.Invoke(new());

                if (!_portal) _portal = GameObject.FindGameObjectWithTag("Portal");
                _portal.SetActive(data.state.phase != Gameplay.GamePhase.Break);
                // mirror for some reason automaticly disables mesh renderer
                _portal.GetComponent<MeshRenderer>().enabled = data.state.phase != Gameplay.GamePhase.Break;
            });
        }
    }
}