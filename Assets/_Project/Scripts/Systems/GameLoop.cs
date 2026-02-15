using System;
using Game.Core.Events;
using Game.Core.Maps;
using Game.Events.GameLoop;
using Game.Events.Boxes;
using Mirror;
using UnityEngine;
using Game.Player;
using System.Linq;

using Random = UnityEngine.Random;
using System.Collections.Generic;

namespace Game.Systems
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
        public double soundtrackBeginTime;

        // why tf are getters can be made readonly
        public readonly float SecondsSinceSoundtrackStarted => (float)(NetworkTime.time - soundtrackBeginTime);

        public GameState(GamePhase phase, int mapIndex, int soundtrackIndex, double soundtrackBeginTime)
        {
            this.phase = phase;
            this.mapIndex = mapIndex;
            this.soundtrackIndex = soundtrackIndex;
            this.soundtrackBeginTime = soundtrackBeginTime;
        }

        public readonly override string ToString()
        {
            return $"{phase.type} | {phase.SecondsSinceEntered} seconds | map: {mapIndex} | ost: {soundtrackIndex}";
        }
    }

    public class GameLoop : NetworkBehaviour
    {
        public GamePhaseInfo breakPhaseInfo;
        public GamePhaseInfo preparationPhaseInfo;
        public GamePhaseInfo matchPhaseInfo;
        public GamePhaseInfo finishPhaseInfo;

        [SyncVar(hook = nameof(OnStateChanged)), ReadOnly] public GameState state;
        private readonly Dictionary<int, int> _lastSoundtrackIndexForMap = new();

        private void OnStateChanged(GameState old, GameState _new)
        {
            EventBus<OnGameStateChange>.Invoke(new() { state = _new });
        }

        private void Start()
        {
            if (!isServer) return;
            state = new(new(GamePhaseType.Break, breakPhaseInfo), -1, -1, 0);
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
            var mapIdx = Random.Range(0, MapPool.maps.Length);
            var conf = MapPool.maps[mapIdx];
            MapLoader.Load(conf);

            int soundtrackIdx;
            if (_lastSoundtrackIndexForMap.TryGetValue(mapIdx, out var lastSoundtrackIndex))
            {
                var newSoundtrackPool = conf.soundtracks.Where((_, idx) => idx != lastSoundtrackIndex).ToArray();
                soundtrackIdx = Array.IndexOf(conf.soundtracks, newSoundtrackPool[Random.Range(0, newSoundtrackPool.Length)]);
                _lastSoundtrackIndexForMap[mapIdx] = soundtrackIdx;
            }
            else
            {
                soundtrackIdx = Random.Range(0, conf.soundtracks.Length);
                _lastSoundtrackIndexForMap.Add(mapIdx, soundtrackIdx);
            }

            state = new(new(GamePhaseType.Preparation, preparationPhaseInfo), mapIdx, soundtrackIdx, NetworkTime.time);
        }

        [Server]
        private void EnterMatch()
        {
            state = new(new(GamePhaseType.Match, matchPhaseInfo), state.mapIndex, state.soundtrackIndex, state.soundtrackBeginTime);

            foreach (var (_, player) in MapLoader.loadedMap.players)
            {
                player.PickRandomItem();
            }

            EventBus<SetBoxSpawnerActive>.Invoke(new() { active = true, resetSpawnedBoxesCounter = true });
        }

        [Server]
        private void EnterFinish()
        {
            state = new(new(GamePhaseType.Finish, finishPhaseInfo), state.mapIndex, state.soundtrackIndex, state.soundtrackBeginTime);
            EventBus<SetBoxSpawnerActive>.Invoke(new() { active = false });

            foreach (var (_, player) in MapLoader.loadedMap.players)
            {
                player.inputLocks++;
                player.hitLocks++;
                player.damageLocks++;
            }
        }

        [Server]
        private void EnterBreak()
        {
            MapLoader.Unload();

            // Reset player & player stats
            foreach (var player in FindObjectsByType<PlayerBase>(FindObjectsSortMode.None))
            {
                player.ResetPlayer();
                player.ResetStats();
                player.inputLocks = 0;
                player.hitLocks = 0;
                player.damageLocks = 0;
            }

            state = new(new(GamePhaseType.Break, breakPhaseInfo), -1, -1, 0);
        }
    }
}