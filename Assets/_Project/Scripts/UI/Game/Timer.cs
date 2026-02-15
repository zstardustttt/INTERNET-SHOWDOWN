using EasyTextEffects;
using Game.Core.Events;
using Game.Events.GameLoop;
using Game.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;

namespace Game.UI.Game
{
    public class Timer : MonoBehaviour
    {
        public TMP_Text timerText;
        public TextEffect textEffect;
        public AudioSource voicelineAudioSource;
        public AudioResource voicelineOneAudioResource;
        public AudioResource voicelineTwoAudioResource;
        public AudioResource voicelineThreeAudioResource;
        public AudioResource voicelineGoAudioResource;
        public AudioResource voicelineTimesUpAudioResource;

        private string _text;
        private string _previousText;
        private GameState _gameState;

        private void Awake()
        {
            _text = string.Empty;
            EventBus<OnGameStateChange>.Listen((data) => _gameState = data.state);
        }

        private void Update()
        {
            if (_gameState.phase.type == GamePhaseType.Finish)
            {
                if (TimerUpdate("Time's up!"))
                {
                    PlayVoiceline(voicelineTimesUpAudioResource);
                }
            }
            else if (_gameState.phase.type == GamePhaseType.Preparation)
            {
                var countdown = Mathf.CeilToInt(_gameState.phase.info.duration - _gameState.phase.SecondsSinceEntered);
                if (TimerUpdate(FormatCountdown(countdown)))
                {
                    if (countdown == 3) PlayVoiceline(voicelineThreeAudioResource);
                    else if (countdown == 2) PlayVoiceline(voicelineTwoAudioResource);
                    else if (countdown == 1) PlayVoiceline(voicelineOneAudioResource);
                    else if (countdown == 0) PlayVoiceline(voicelineGoAudioResource);
                }
            }
            else
            {
                var countdown = Mathf.CeilToInt(_gameState.phase.info.duration - _gameState.phase.SecondsSinceEntered);
                TimerUpdate(FormatCountdown(countdown));
            }
        }

        private string FormatCountdown(int countdown)
        {
            var minutes = countdown / 60;
            var seconds = countdown % 60;
            return minutes == 0 ? seconds.ToString() : string.Format("{0}:{1:00}", minutes, seconds);
        }

        private bool TimerUpdate(string newText)
        {
            if (_text == newText) return false;

            _previousText = _text;
            _text = newText;

            var finalText = "";
            if (_previousText.Length == _text.Length)
            {
                var previousCharacterChanged = false;
                for (int i = 0; i < _text.Length; i++)
                {
                    var character = _text[i];
                    if (_previousText[i] == character)
                    {
                        if (previousCharacterChanged)
                        {
                            finalText += "</link>";
                            previousCharacterChanged = false;
                        }

                        finalText += character;
                        continue;
                    }

                    if (!previousCharacterChanged)
                    {
                        finalText += $"<link=AMov+ACol>{character}";
                        previousCharacterChanged = true;
                    }
                    else finalText += character;
                }

                if (previousCharacterChanged)
                    finalText += "</link>";
            }
            else if (_text.Length != 0)
            {
                finalText = _text.Insert(_text.Length - 1, "<link=AMov+ACol>");
                finalText = finalText.Insert(finalText.Length, "</link>");
            }
            else return true;

            timerText.text = finalText;
            timerText.Rebuild(UnityEngine.UI.CanvasUpdate.PreRender);
            textEffect.AddTagEffects(timerText.textInfo.linkInfo, timerText.textInfo.linkCount);
            textEffect.StartManualTagEffects();
            textEffect.StartManualEffect("Bounce");

            return true;
        }

        public void PlayVoiceline(AudioResource voiceline)
        {
            voicelineAudioSource.resource = voiceline;
            voicelineAudioSource.Play();
        }
    }
}
