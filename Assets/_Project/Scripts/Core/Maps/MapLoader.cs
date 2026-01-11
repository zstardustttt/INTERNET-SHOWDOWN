using System;
using System.Collections.Generic;
using Game.Core.Events;
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

    // Only server should interact with this class
    // I assume that if the client attempts to interact with this class
    // an exception will be triggered and Mirror will straight up blast his ass off the server
    // UPDATE: even if the client gets around server attributes and interacts with this class
    // client isn't getting disconnected, but absolutely nothing happens to the server and other clients so thats fine
    public static class MapLoader
    {
        public static Map loadedMap;
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
        }

        [Server]
        public static bool TryMoveGameObjectToMap(GameObject go)
        {
            if (loadedMap == null || !loadedMap.scene.IsValid() || go.scene == loadedMap.scene)
                return false;

            SceneManager.MoveGameObjectToScene(go, loadedMap.scene);
            if (go.TryGetComponent(out PlayerBase player)) loadedMap.players.Add(player);
            return true;
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
            if (scene.path != loadedMap.config.sceneName) return;
            loadedMap.scene = scene;
            loadedMap.info = Object.FindFirstObjectByType<MapInfo>();
        }

        [Server]
        public static void Load(MapConfig config)
        {
            if (!config)
            {
                Debug.LogError("Specified MapInfo is null");
                return;
            }

            if (loadedMap != null)
            {
                Debug.LogError("There is a map already loaded");
                return;
            }

            loadedMap = new() { config = config, players = new() };
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

            // Move every client back to lobby
            foreach (var (_, conn) in NetworkServer.connections)
            {
                SceneManager.MoveGameObjectToScene(conn.identity.gameObject, SceneManager.GetSceneByName("Lobby"));
                conn.Send<ServerMovePlayer>(new() { position = Vector3.zero });
            }

            SceneManager.UnloadSceneAsync(loadedMap.scene);
            loadedMap = null;
        }
    }
}