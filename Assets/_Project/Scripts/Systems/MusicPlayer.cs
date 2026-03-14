using Game.Core.Events;
using Game.Core.Maps;
using Game.GameLoop;
using Game.GameLoop.Events;
using Game.Player.Online.Events;
using UnityEngine;

namespace Game.Systems
{
    [RequireComponent(typeof(AudioSource))]
    public class MusicPlayer : MonoBehaviour
    {
        public AudioSource source;
        [Range(0f, 1f)] public float volume;

        private Soundtrack _soundtrack;
        private GameState _currentState;
        private GameState _previousState;
        private bool _musicRequested;
        private float _soundtrackOffset;

        private void OnValidate()
        {
            source = GetComponent<AudioSource>();
        }

        private void Awake()
        {
            EventBus<OnGameStateChange>.Listen((data) =>
            {
                var state = data.state;
                _currentState = data.state;

                if (state.mapIndex != -1 && state.soundtrackIndex != -1)
                {
                    if (state.soundtrackIndex == _previousState.soundtrackIndex)
                    {
                        _soundtrackOffset = state.soundtrackOffset;
                        if (source.clip == _soundtrack.clip)
                            source.time = _currentState.phase.SecondsSinceEntered + _soundtrackOffset;
                    }
                    else
                    {
                        _soundtrack = MapPool.maps[state.mapIndex].soundtracks[state.soundtrackIndex];
                        _soundtrack.clip.LoadAudioData();
                        _soundtrackOffset = state.soundtrackOffset;
                    }
                }

                _previousState = data.state;
            });
            EventBus<OnLocalPlayerAddedToMap>.Listen((_) => { _musicRequested = true; });
            EventBus<OnLocalPlayerRemovedFromMap>.Listen((_) => StopMatchMusic());
        }

        private void Update()
        {
            if (!_musicRequested || !_soundtrack || _soundtrack.clip.loadState != AudioDataLoadState.Loaded) return;

            source.clip = _soundtrack.clip;
            source.volume = _soundtrack.volume * volume;
            source.Play();
            source.time = _currentState.phase.SecondsSinceEntered + _soundtrackOffset;

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