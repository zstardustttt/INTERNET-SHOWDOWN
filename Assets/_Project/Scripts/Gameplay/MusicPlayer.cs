using Game.Core.Events;
using Game.Core.Maps;
using Game.Events.GameLoop;
using Game.Events.MusicPlayer;
using UnityEngine;

namespace Game.Gameplay
{
    [RequireComponent(typeof(AudioSource))]
    public class MusicPlayer : MonoBehaviour
    {
        public AudioSource source;

        [HideInInspector] public GameState state;

        private void OnValidate()
        {
            source = GetComponent<AudioSource>();
        }

        private void Awake()
        {
            EventBus<OnGameStateChange>.Listen((data) => state = data.state);
            EventBus<RequestMatchMusic>.Listen((_) => PlayMatchMusic());
            EventBus<StopMatchMusic>.Listen((_) => StopMatchMusic());
        }

        private void PlayMatchMusic()
        {
            if (state.mapIndex == -1 || state.soundtrackIndex == -1)
            {
                Debug.LogWarning("Cant play music right now");
                return;
            }

            source.clip = MapPool.maps[state.mapIndex].soundtracks[state.soundtrackIndex];
            source.Play();
            source.time = state.SecondsSinceTimerStarted;
        }

        private void StopMatchMusic()
        {
            source.Stop();
        }
    }
}