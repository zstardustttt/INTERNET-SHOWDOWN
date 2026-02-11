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

namespace Game.Systems
{
    // TODO: refactor state switching
    public enum GamePhase
    {
        Break,
        Preparation,
        Match,
        Finish
    }

    [Serializable]
    public struct GameState
    {
        public GamePhase phase;
        public float phaseDuration;
        public double timerBeginTime;
        public double soundtrackBeginTime;
        public int mapIndex;
        public int soundtrackIndex;

        // why tf are getters can be made readonly
        public readonly float SecondsSinceTimerStarted => (float)(NetworkTime.time - timerBeginTime);
        public readonly float SecondsSinceSoundtrackStarted => (float)(NetworkTime.time - soundtrackBeginTime);

        public GameState(GamePhase phase, float phaseDuration, int mapIndex, int soundtrackIndex, double timerBeginTime, double soundtrackBeginTime)
        {
            this.phase = phase;
            this.phaseDuration = phaseDuration;
            this.mapIndex = mapIndex;
            this.soundtrackIndex = soundtrackIndex;
            this.timerBeginTime = timerBeginTime;
            this.soundtrackBeginTime = soundtrackBeginTime;
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
        public float finishDuration;

        [SyncVar(hook = nameof(OnStateChanged)), ReadOnly] public GameState state;

        private GameState _lastMatchState;

        private void OnStateChanged(GameState old, GameState _new)
        {
            EventBus<OnGameStateChange>.Invoke(new() { state = _new });
        }

        private void Start()
        {
            if (!isServer) return;
            state = new(GamePhase.Break, breakDuration, -1, -1, NetworkTime.time, 0);
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

            if (state.SecondsSinceTimerStarted >= state.phaseDuration)
            {
                SwitchToNextState();
            }

            /*if (state.phase == GamePhase.Break && state.SecondsSinceTimerStarted >= breakDuration)
                EnterPreparation();
            else if (state.phase == GamePhase.Preparation && state.SecondsSinceTimerStarted >= preparationDuration)
                EnterMatch();
            else if (state.phase == GamePhase.Match && state.SecondsSinceTimerStarted >= preparationDuration + matchDuration)
                EnterFinish();
            else if (state.phase == GamePhase.Finish && state.SecondsSinceTimerStarted >= preparationDuration + matchDuration + finishDuration)
                EnterBreak();*/
        }

        [Server]
        private void SwitchToNextState()
        {
            if (state.phase == GamePhase.Break)
                EnterPreparation();
            else if (state.phase == GamePhase.Preparation)
                EnterMatch();
            else if (state.phase == GamePhase.Match)
                EnterFinish();
            else if (state.phase == GamePhase.Finish)
                EnterBreak();
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
            }

            state = new(GamePhase.Break, breakDuration, -1, -1, NetworkTime.time, 0);
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

            state = new(GamePhase.Preparation, preparationDuration, mapIdx, soundtrackIdx, NetworkTime.time, NetworkTime.time);
            _lastMatchState = state;
        }

        [Server]
        private void EnterMatch()
        {
            state = new(GamePhase.Match, matchDuration, state.mapIndex, state.soundtrackIndex, NetworkTime.time, state.soundtrackBeginTime);
            _lastMatchState = state;

            foreach (var (_, player) in MapLoader.loadedMap.players)
            {
                player.PickRandomItem();
            }

            EventBus<SetBoxSpawnerActive>.Invoke(new() { active = true, resetSpawnedBoxesCounter = true });
        }

        [Server]
        private void EnterFinish()
        {
            state = new(GamePhase.Finish, finishDuration, state.mapIndex, state.soundtrackIndex, NetworkTime.time, state.soundtrackBeginTime);
            EventBus<SetBoxSpawnerActive>.Invoke(new() { active = false });

            // Lock item usage on finish & activate invincibility?
            foreach (var (_, player) in MapLoader.loadedMap.players)
            {
                player.motorLocks++;
            }
        }
    }
}