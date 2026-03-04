using System;
using System.Collections.Generic;
using Game.Core.Events;
using Game.Maps.Events;
using Game.Player;
using Game.Player.Events;
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
            _onDestroyPlayerListenerGuid = EventBus<OnPlayerDestroy>.Listen((data) =>
            {
                if (loadedMap == null) return;
                loadedMap.players.Remove(data.guid);
            });
        }

        [Server]
        public static void Stop()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            EventBus<OnPlayerDestroy>.TryCancel(_onDestroyPlayerListenerGuid);
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
                EventBus<OnAddPlayerToMap>.Invoke(new() { player = player });
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
        public static AsyncOperation Unload()
        {
            if (loadedMap == null || !loadedMap.scene.IsValid())
            {
                Debug.LogError("Map is already unloaded");
                return null;
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
            }

            EventBus<OnUnloadMap>.Invoke(new());

            var op = SceneManager.UnloadSceneAsync(loadedMap.scene);
            op.completed += (_) => EventBus<OnMapUnloaded>.Invoke(new());

            loadedMap = null;
            return op;
        }

        [Server]
        public static bool IsPlayerOnMap(PlayerBase player)
        {
            if (loadedMap == null) return false;
            return loadedMap.players.ContainsKey(player.playerGuid);
        }

        [Server]
        public static GameObject NetworkSpawnOnMap(GameObject obj, Vector3 position, Quaternion rotation)
        {
            if (loadedMap == null)
                throw new("Can't spawn object on map. Map isn't loaded");

            var newObject = Object.Instantiate(obj, position, rotation, new InstantiateParameters()
            {
                scene = loadedMap.scene
            });
            NetworkServer.Spawn(newObject);

            return newObject;
        }
    }
}