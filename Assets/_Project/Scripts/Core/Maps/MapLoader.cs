using System;
using System.Collections.Generic;
using Game.Core.Events;
using Game.Events.MapLoader;
using Game.Events.Player;
using Game.Network.Messages;
using Game.Player;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

using Object = UnityEngine.Object;

namespace Game.Core.Maps
{
    public class Map
    {
        public Scene scene;
        public MapInfo info;
        public MapConfig config;
        public List<PlayerBase> players;
    }

    public static class MapLoader
    {
        public static Map loadedMap;
        public static MapConfig loadingMapConfig;
        private static Guid _onDestroyPlayerListenerGuid;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            loadedMap = null;
        }

        [Server]
        public static void Init()
        {
            SceneManager.sceneLoaded += SceneLoaded;
            _onDestroyPlayerListenerGuid = EventBus<OnDestroyPlayer>.Listen((_) => CleanupDestroyedPlayer());
        }

        [Server]
        public static void Stop()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            EventBus<OnDestroyPlayer>.TryCancel(_onDestroyPlayerListenerGuid);
            loadedMap = null;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!loadingMapConfig || scene.path != loadingMapConfig.sceneName) return;
            loadedMap = new()
            {
                scene = scene,
                info = Object.FindFirstObjectByType<MapInfo>(),
                config = loadingMapConfig,
                players = new(),
            };

            loadingMapConfig = null;
            EventBus<OnPlayersOnMapUpdated>.Invoke(new());
        }

        [Server]
        private static void CleanupDestroyedPlayer()
        {
            if (loadedMap == null) return;

            var index = 0;
            for (int i = 0; i < loadedMap.players.Count; i++)
            {
                if (loadedMap.players[i]) continue;
                index = i;
                break;
            }

            loadedMap.players.RemoveAt(index);

            EventBus<OnPlayersOnMapUpdated>.Invoke(new());
        }

        [Server]
        public static bool TryMoveGameObjectToMap(GameObject go)
        {
            if (loadedMap == null || !loadedMap.scene.IsValid() || go.scene == loadedMap.scene)
                return false;

            SceneManager.MoveGameObjectToScene(go, loadedMap.scene);
            if (go.TryGetComponent(out PlayerBase player))
            {
                loadedMap.players.Add(player);
                EventBus<OnPlayersOnMapUpdated>.Invoke(new());
            }
            return true;
        }

        [Server]
        public static void Load(MapConfig config)
        {
            if (!config)
            {
                Debug.LogError("Specified MapConfig is null");
                return;
            }

            if (loadedMap != null)
            {
                Debug.LogError("There is a map already loaded");
                return;
            }

            if (loadingMapConfig)
            {
                Debug.LogError("Map is already loading");
                return;
            }

            loadingMapConfig = config;
            SceneManager.LoadScene(config.name, LoadSceneMode.Additive);
        }

        [Server]
        public static void Unload()
        {
            if (loadedMap == null || !loadedMap.scene.IsValid())
            {
                Debug.LogError("Map is already unloaded");
                return;
            }

            // Move every player back to lobby
            foreach (var player in loadedMap.players)
            {
                SceneManager.MoveGameObjectToScene(player.gameObject, SceneManager.GetSceneByName("Lobby"));
                player.connectionToClient?.Send(new SceneMessage()
                {
                    sceneName = loadedMap.config.sceneName,
                    sceneOperation = SceneOperation.UnloadAdditive
                });

                player.ServerMovePlayer(Vector3.zero);
            }

            SceneManager.UnloadSceneAsync(loadedMap.scene);
            loadedMap = null;
        }
    }
}