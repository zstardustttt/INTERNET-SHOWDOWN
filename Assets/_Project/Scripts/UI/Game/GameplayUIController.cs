using System.Collections;
using Game.Core.Events;
using Game.Events.GameLoop;
using Game.Events.UI;
using Game.Systems;
using UnityEngine;

namespace Game.UI.Game
{
    public class GameplayUIController : MonoBehaviour
    {
        private GameState _gameState;
        public CanvasGroup canvasGroup;

        [Header("Indicators")]
        public CanvasGroup hitIndicator;
        public CanvasGroup damageIndicator;
        public CanvasGroup pureKillIndicator;
        public CanvasGroup unpureKillIndicator;

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

            EventBus<HitIndicatorRequest>.Listen((_) => HitIndicatorAnimation());
            EventBus<DamageIndicatorRequest>.Listen((_) => DamageIndicatorAnimation());
            EventBus<PureKillIndicatorRequest>.Listen((_) => PureKillIndicatorAnimation());
            EventBus<UnpureKillIndicatorRequest>.Listen((_) => UnpureKillIndicatorAnimation());
        }

        private Coroutine _hitIndicatorCoroutine;
        private Coroutine _damageIndicatorCoroutine;
        private Coroutine _pureKillIndicatorCoroutine;
        private Coroutine _unpureKillIndicatorCoroutine;

        private void HitIndicatorAnimation()
        {
            if (_hitIndicatorCoroutine != null) StopCoroutine(_hitIndicatorCoroutine);
            _hitIndicatorCoroutine = StartCoroutine(CO_IndicatorAnimation(hitIndicator, 0.5f));
        }

        private void DamageIndicatorAnimation()
        {
            if (_damageIndicatorCoroutine != null) StopCoroutine(_damageIndicatorCoroutine);
            _damageIndicatorCoroutine = StartCoroutine(CO_IndicatorAnimation(damageIndicator, 0.5f));
        }

        private void PureKillIndicatorAnimation()
        {
            if (_pureKillIndicatorCoroutine != null) StopCoroutine(_pureKillIndicatorCoroutine);
            _pureKillIndicatorCoroutine = StartCoroutine(CO_IndicatorAnimation(pureKillIndicator, 1f));
        }

        private void UnpureKillIndicatorAnimation()
        {
            if (_unpureKillIndicatorCoroutine != null) StopCoroutine(_unpureKillIndicatorCoroutine);
            _unpureKillIndicatorCoroutine = StartCoroutine(CO_IndicatorAnimation(unpureKillIndicator, 1f));
        }

        private IEnumerator CO_IndicatorAnimation(CanvasGroup group, float duration)
        {
            group.alpha = 1f;
            while (group.alpha > 0f)
            {
                group.alpha -= Time.deltaTime / duration;
                yield return null;
            }
        }

        private void SwitchUI(bool enable)
        {
            canvasGroup.alpha = enable ? 1f : 0f;
            canvasGroup.blocksRaycasts = enable;
            canvasGroup.interactable = enable;
        }
    }
}