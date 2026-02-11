using EasyTextEffects;
using Game.Core.Events;
using Game.Events.GameLoop;
using Game.Systems;
using TMPro;
using UnityEngine;

namespace Game.UI.Game
{
    public class Timer : MonoBehaviour
    {
        public TMP_Text timerText;
        public TextEffect textEffect;
        private GameState _gameState;

        private string _text;
        private string _previousText;

        private void Awake()
        {
            _text = string.Empty;
            EventBus<OnGameStateChange>.Listen((data) => _gameState = data.state);
        }

        private void Update()
        {
            if (_gameState.phase.type == GamePhaseType.Finish)
            {
                TimerUpdate("Time's up!");
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

        private void TimerUpdate(string newText)
        {
            if (_text == newText) return;

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
            else return;

            timerText.text = finalText;
            timerText.Rebuild(UnityEngine.UI.CanvasUpdate.PreRender);
            textEffect.AddTagEffects(timerText.textInfo.linkInfo, timerText.textInfo.linkCount);
            textEffect.StartManualTagEffects();
            textEffect.StartManualEffect("Bounce");
        }
    }
}
