using System;
using Game.Core.Events;
using Game.Core.Maps;
using Game.Events.GameLoop;
using Game.Events.BoxSpawner;
using Mirror;
using UnityEngine;
using Game.Player;
using System.Linq;

using Random = UnityEngine.Random;

namespace Game.Gameplay
{
    public enum GamePhase
    {
        Break,
        Preparation,
        Match
    }

    [Serializable]
    public struct GameState
    {
        public GamePhase phase;
        public double timerBeginTime;
        public int mapIndex;
        public int soundtrackIndex;

        // why tf are getters can be made readonly
        public readonly float SecondsSinceTimerStarted => (float)(NetworkTime.time - timerBeginTime);

        public GameState(GamePhase phase, int mapIndex, int soundtrackIndex, double timerBeginTime)
        {
            this.phase = phase;
            this.mapIndex = mapIndex;
            this.soundtrackIndex = soundtrackIndex;
            this.timerBeginTime = timerBeginTime;
        }

        public readonly override string ToString()
        {
            return $"{phase} | {SecondsSinceTimerStarted} seconds | map: {mapIndex} | ost: {soundtrackIndex}";
        }
    }

    public class GameLoop : NetworkBehaviour
    {
        public float breakDuration;
        public float preparationDuration;
        public float matchDuration;

        [SyncVar(hook = nameof(OnStateChanged)), ReadOnly] public GameState state;

        private GameState _lastMatchState;

        private void OnStateChanged(GameState old, GameState _new)
        {
            EventBus<OnGameStateChange>.Invoke(new() { state = _new });
        }

        private void Start()
        {
            if (!isServer) return;
            state = new(GamePhase.Break, -1, -1, NetworkTime.time);
        }

        private void Update()
        {
            if (!isServer) return;

#if DEBUG
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                if (state.phase == GamePhase.Break) EnterPreparation();
                else EnterBreak();
            }
#endif

            if (state.phase == GamePhase.Break && state.SecondsSinceTimerStarted >= breakDuration)
                EnterPreparation();
            else if (state.phase == GamePhase.Preparation && state.SecondsSinceTimerStarted >= preparationDuration)
                EnterMatch();
            else if (state.phase == GamePhase.Match && state.SecondsSinceTimerStarted >= preparationDuration + matchDuration)
                EnterBreak();
        }

        [Server]
        private void EnterBreak()
        {
            EventBus<SetBoxSpawnerActive>.Invoke(new() { active = false });
            MapLoader.Unload();

            // Reset player stats
            foreach (var player in FindObjectsByType<PlayerBase>(FindObjectsSortMode.None))
            {
                player.ResetPlayer();
            }

            state = new(GamePhase.Break, -1, -1, NetworkTime.time);
        }

        [Server]
        private void EnterPreparation()
        {
            var mapIdx = Random.Range(0, MapPool.maps.Length);
            var conf = MapPool.maps[mapIdx];
            MapLoader.Load(conf);

            int soundtrackIdx;
            if (mapIdx == _lastMatchState.mapIndex)
            {
                var newSoundtrackPool = conf.soundtracks.Where((_, idx) => idx != _lastMatchState.soundtrackIndex).ToArray();
                soundtrackIdx = Array.IndexOf(conf.soundtracks, newSoundtrackPool[Random.Range(0, newSoundtrackPool.Length)]);
            }
            else soundtrackIdx = Random.Range(0, conf.soundtracks.Length);

            state = new(GamePhase.Preparation, mapIdx, soundtrackIdx, NetworkTime.time);
            _lastMatchState = state;
        }

        [Server]
        private void EnterMatch()
        {
            state = new(GamePhase.Match, state.mapIndex, state.soundtrackIndex, state.timerBeginTime);
            _lastMatchState = state;

            EventBus<SetBoxSpawnerActive>.Invoke(new() { active = true });
        }
    }
}