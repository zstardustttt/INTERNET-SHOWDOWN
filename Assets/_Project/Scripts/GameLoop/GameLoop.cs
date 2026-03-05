using System;
using Game.Core.Events;
using Game.Core.Maps;
using Mirror;
using UnityEngine;
using Game.Player;
using System.Linq;

using Random = UnityEngine.Random;
using System.Collections.Generic;
using Game.GameLoop.Events;
using Game.Core.Lobby;

namespace Game.GameLoop
{
    [Serializable]
    public struct GamePhaseInfo
    {
        public float duration;
        public bool saveStats;
        public bool loadStats;
        public bool activatePortal;
    }

    public enum GamePhaseType
    {
        Break,
        Preparation,
        Match,
        Finish
    }

    public struct GamePhase
    {
        public GamePhaseType type;
        public GamePhaseInfo info;
        public double enterTime;

        public readonly float SecondsSinceEntered => (float)(NetworkTime.time - enterTime);

        public GamePhase(GamePhaseType type, GamePhaseInfo info)
        {
            this.type = type;
            this.info = info;
            enterTime = NetworkTime.time;
        }
    }

    [Serializable]
    public struct GameState
    {
        public GamePhase phase;
        public int mapIndex;
        public int soundtrackIndex;
        public float soundtrackOffset;

        public GameState(GamePhase phase, int mapIndex, int soundtrackIndex, float soundtrackOffset)
        {
            this.phase = phase;
            this.mapIndex = mapIndex;
            this.soundtrackIndex = soundtrackIndex;
            this.soundtrackOffset = soundtrackOffset;
        }

        public readonly override string ToString()
        {
            return $"{phase.type} | {phase.SecondsSinceEntered} seconds | map: {mapIndex} | ost: {soundtrackIndex}";
        }
    }

    public class GameLoop : NetworkBehaviour
    {
        public LobbyInfo lobbyInfo;

        [Space(9)]
        public GamePhaseInfo breakPhaseInfo;
        public GamePhaseInfo preparationPhaseInfo;
        public GamePhaseInfo matchPhaseInfo;
        public GamePhaseInfo finishPhaseInfo;

        [SyncVar(hook = nameof(OnStateChanged)), ReadOnly] public GameState state;
        private readonly Dictionary<int, int> _lastSoundtrackIndexForMap = new();
        private int _mapIdx;

        private void OnStateChanged(GameState old, GameState _new)
        {
            EventBus<OnGameStateChange>.Invoke(new() { state = _new });
        }

        private void Start()
        {
            if (!isServer) return;
            _mapIdx = -1;
            state = new(new(GamePhaseType.Break, breakPhaseInfo), -1, -1, 0f);
        }

        private void Update()
        {
            if (!isServer) return;

#if DEBUG
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                SwitchToNextState();
            }
#endif

            if (state.phase.SecondsSinceEntered >= state.phase.info.duration)
            {
                SwitchToNextState();
            }
        }

        [Server]
        private void SwitchToNextState()
        {
            if (state.phase.type == GamePhaseType.Break)
                EnterPreparation();
            else if (state.phase.type == GamePhaseType.Preparation)
                EnterMatch();
            else if (state.phase.type == GamePhaseType.Match)
                EnterFinish();
            else if (state.phase.type == GamePhaseType.Finish)
                EnterBreak();
        }

        [Server]
        private void EnterPreparation()
        {
            if (_mapIdx == -1) _mapIdx = Random.Range(0, MapPool.maps.Length);
            else
            {
                var newMapPool = MapPool.maps.Where((_, idx) => idx != _mapIdx).ToArray();
                _mapIdx = Array.IndexOf(MapPool.maps, newMapPool[Random.Range(0, newMapPool.Length)]);
            }

            var conf = MapPool.maps[_mapIdx];
            MapLoader.Load(conf);

            int soundtrackIdx;
            if (_lastSoundtrackIndexForMap.TryGetValue(_mapIdx, out var lastSoundtrackIndex))
            {
                var newSoundtrackPool = conf.soundtracks.Where((_, idx) => idx != lastSoundtrackIndex).ToArray();
                soundtrackIdx = Array.IndexOf(conf.soundtracks, newSoundtrackPool[Random.Range(0, newSoundtrackPool.Length)]);
                _lastSoundtrackIndexForMap[_mapIdx] = soundtrackIdx;
            }
            else
            {
                soundtrackIdx = Random.Range(0, conf.soundtracks.Length);
                _lastSoundtrackIndexForMap.Add(_mapIdx, soundtrackIdx);
            }

            state = new(new(GamePhaseType.Preparation, preparationPhaseInfo), _mapIdx, soundtrackIdx, 0f);
        }

        [Server]
        private void EnterMatch()
        {
            state = new(new(GamePhaseType.Match, matchPhaseInfo), state.mapIndex, state.soundtrackIndex, preparationPhaseInfo.duration);

            foreach (var (_, player) in MapLoader.loadedMap.players)
            {
                player.itemModule.PickRandomItem();
            }
        }

        [Server]
        private void EnterFinish()
        {
            state = new(new(GamePhaseType.Finish, finishPhaseInfo), state.mapIndex, state.soundtrackIndex, preparationPhaseInfo.duration + matchPhaseInfo.duration);

            foreach (var (_, player) in MapLoader.loadedMap.players)
            {
                player.locks.Lock(PlayerLock.Input, PlayerLock.Hit, PlayerLock.Health);
                player.healthModule.ClearInvincibility();
            }
        }

        [Server]
        private void EnterBreak()
        {
            // Reset player & player stats
            foreach (var (_, player) in MapLoader.loadedMap.players)
            {
                player.ServerMovePlayer(lobbyInfo.spawnArea.RandomSampleArea(Space.World));

                player.healthModule.ResetHealth();
                player.itemModule.ResetItem();
                player.deathModule.Respawn();
                player.ResetStats();
            }

            MapLoader.Unload().completed += (_) =>
            {
                // Unlock everything only once assured that every object from the map has been destroyed
                foreach (var player in FindObjectsByType<PlayerBase>(FindObjectsSortMode.None))
                {
                    player.locks.Drop(PlayerLocks.all);
                }
            };

            state = new(new(GamePhaseType.Break, breakPhaseInfo), -1, -1, 0f);
        }
    }
}