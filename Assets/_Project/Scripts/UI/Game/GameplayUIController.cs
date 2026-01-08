using Game.Core.Events;
using Game.Events.GameLoop;
using Game.Events.UI;
using Game.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Game
{
    public class GameplayUIController : MonoBehaviour
    {
        public CanvasGroup canvasGroup;
        public Slider health;

        private GameState _gameState;
        private bool _uiSwitchRequested;

        private void Awake()
        {
            EventBus<OnGameStateChange>.Listen((data) =>
            {
                _gameState = data.state;
                if (_gameState.phase == GamePhase.Match && _uiSwitchRequested)
                    SwitchUI(true);
                else SwitchUI(false);

                _uiSwitchRequested = false;
            });

            EventBus<RequestGameplayUI>.Listen((_) =>
            {
                if (_gameState.phase == GamePhase.Match) SwitchUI(true);
                else _uiSwitchRequested = true;
            });

            EventBus<OnHealthUpdate>.Listen(OnHealthUpdate);
        }

        private void SwitchUI(bool enable)
        {
            canvasGroup.alpha = enable ? 1f : 0f;
            canvasGroup.blocksRaycasts = enable;
            canvasGroup.interactable = enable;
        }

        private void OnHealthUpdate(OnHealthUpdate data)
        {
            health.maxValue = data.maxHealth;
            health.value = data.health;
        }
    }
}