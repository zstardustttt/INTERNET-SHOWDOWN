using System;
using Game.Core.Events;
using Game.Core.Maps;
using Game.Events.GameLoop;
using Game.Events.BoxSpawner;
using Mirror;
using UnityEngine;
using Game.Player;

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
        public long timerBeginTicks;
        public int mapIndex;
        public int soundtrackIndex;

        // why tf are getters can be made readonly
        public readonly float SecondsSinceTimerStarted => (float)new TimeSpan(DateTime.Now.Ticks - timerBeginTicks).TotalSeconds;

        public GameState(GamePhase phase, int mapIndex, int soundtrackIndex, long timerBeginTicks)
        {
            this.phase = phase;
            this.mapIndex = mapIndex;
            this.soundtrackIndex = soundtrackIndex;
            this.timerBeginTicks = timerBeginTicks;
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

        private void OnStateChanged(GameState old, GameState _new)
        {
            EventBus<OnGameStateChange>.Invoke(new() { state = _new });
        }

        private void Start()
        {
            if (!isServer) return;
            state = new(GamePhase.Break, -1, -1, DateTime.Now.Ticks);
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

            foreach (var itemManager in FindObjectsByType<PlayerBase>(FindObjectsSortMode.None))
            {
                itemManager.itemIndex = -1;
            }

            state = new(GamePhase.Break, -1, -1, DateTime.Now.Ticks);
        }

        [Server]
        private void EnterPreparation()
        {
            var idx = UnityEngine.Random.Range(0, MapPool.maps.Length);
            var conf = MapPool.maps[idx];
            MapLoader.Load(conf);

            state = new(GamePhase.Preparation, idx, UnityEngine.Random.Range(0, conf.soundtracks.Length), DateTime.Now.Ticks);
        }

        [Server]
        private void EnterMatch()
        {
            state = new(GamePhase.Match, state.mapIndex, state.soundtrackIndex, state.timerBeginTicks);
            EventBus<SetBoxSpawnerActive>.Invoke(new() { active = true });
        }
    }
}