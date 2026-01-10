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

        private Soundtrack _soundtrack;
        private GameState _state;
        private bool _musicRequested;

        private void OnValidate()
        {
            source = GetComponent<AudioSource>();
        }

        private void Awake()
        {
            EventBus<OnGameStateChange>.Listen((data) =>
            {
                _state = data.state;

                if (_state.mapIndex == -1 || _state.soundtrackIndex == -1) return;
                _soundtrack = MapPool.maps[_state.mapIndex].soundtracks[_state.soundtrackIndex];
                _soundtrack.clip.LoadAudioData();
            });
            EventBus<RequestMatchMusic>.Listen((_) => { _musicRequested = true; });
            EventBus<StopMatchMusic>.Listen((_) => StopMatchMusic());
        }

        private void Update()
        {
            if (!_musicRequested || !_soundtrack || _soundtrack.clip.loadState != AudioDataLoadState.Loaded) return;

            source.clip = _soundtrack.clip;
            source.volume = _soundtrack.volume * volume;
            source.Play();
            source.time = _state.SecondsSinceTimerStarted;

            _musicRequested = false;
        }

        private void StopMatchMusic()
        {
            source.Stop();
            _soundtrack = null;
            _musicRequested = false;
        }
    }
}