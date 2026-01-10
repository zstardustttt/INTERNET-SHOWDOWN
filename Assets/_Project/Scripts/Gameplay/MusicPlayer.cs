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
        [Range(0f, 1f)] public float volume;

        [HideInInspector] public GameState state;
        private Soundtrack _soundtrack;

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

        private void Update()
        {
            if (!_soundtrack) return;
            if (_soundtrack.clip.loadState != AudioDataLoadState.Loaded) return;
            if (source.isPlaying) return;

            source.clip = _soundtrack.clip;
            source.volume = _soundtrack.volume * volume;
            source.Play();
            source.time = state.SecondsSinceTimerStarted;
        }

        private void PlayMatchMusic()
        {
            if (state.mapIndex == -1 || state.soundtrackIndex == -1)
            {
                Debug.LogWarning("Cant play music right now");
                return;
            }

            _soundtrack = MapPool.maps[state.mapIndex].soundtracks[state.soundtrackIndex];
            _soundtrack.clip.LoadAudioData();
        }

        private void StopMatchMusic()
        {
            source.Stop();
            _soundtrack = null;
        }
    }
}