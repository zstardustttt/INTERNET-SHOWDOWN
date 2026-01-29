using System;
using System.Collections.Generic;
using Game.Core.Events;
using Game.Events.MapLoader;
using Game.Events.Player;
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
        public Dictionary<string, PlayerBase> players;
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
            _onDestroyPlayerListenerGuid = EventBus<OnDestroyPlayer>.Listen((data) =>
            {
                if (loadedMap == null) return;
                loadedMap.players.Remove(data.guid);
            });
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
        }

        [Server]
        public static bool TryMoveGameObjectToMap(GameObject go)
        {
            if (loadedMap == null || !loadedMap.scene.IsValid() || go.scene == loadedMap.scene)
                return false;

            SceneManager.MoveGameObjectToScene(go, loadedMap.scene);
            if (go.TryGetComponent(out PlayerBase player))
            {
                loadedMap.players.Add(player.playerGuid, player);
                EventBus<OnAddPlayerOnMap>.Invoke(new() { player = player });
                player.damageReceiver.Register(new Guid(player.playerGuid));
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
            foreach (var (_, player) in loadedMap.players)
            {
                SceneManager.MoveGameObjectToScene(player.gameObject, SceneManager.GetSceneByName("Lobby"));
                player.connectionToClient?.Send(new SceneMessage()
                {
                    sceneName = loadedMap.config.sceneName,
                    sceneOperation = SceneOperation.UnloadAdditive
                });

                player.ServerMovePlayer(Vector3.zero);
                player.ResetPlayer();
                player.damageReceiver.Unregister();
            }

            EventBus<OnUnloadMap>.Invoke(new());

            SceneManager.UnloadSceneAsync(loadedMap.scene);
            loadedMap = null;
        }

        [Server]
        public static bool IsPlayerOnMap(PlayerBase player)
        {
            if (loadedMap == null) return false;
            return loadedMap.players.ContainsKey(player.playerGuid);
        }
    }
}