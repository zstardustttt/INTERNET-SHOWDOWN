using System.Collections;
using Game.Core.Events;
using Game.Events.UI;
using Game.GameLoop;
using Game.GameLoop.Events;
using Game.Player.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Game
{
    public class GameplayUIController : MonoBehaviour
    {
        private GameState _gameState;
        public CanvasGroup canvasGroup;
        public Image respawnEffect;
        public float respawnEffectDuration;

        [Header("Indicators")]
        public CanvasGroup damageIndicator;
        public CanvasGroup pureKillIndicator;
        public CanvasGroup unpureKillIndicator;

        private bool _uiSwitchRequested;

        private void Awake()
        {
            respawnEffect.gameObject.SetActive(false);

            EventBus<OnGameStateChange>.Listen((data) =>
            {
                _gameState = data.state;

                if (_gameState.phase.type == GamePhaseType.Finish) return;

                if (_gameState.phase.type == GamePhaseType.Match && _uiSwitchRequested)
                    SwitchUI(true);
                else SwitchUI(false);

                _uiSwitchRequested = false;
            });

            EventBus<RequestGameplayUI>.Listen((_) =>
            {
                if (_gameState.phase.type == GamePhaseType.Match) SwitchUI(true);
                else _uiSwitchRequested = true;
            });

            EventBus<OnPlayerStatsChanged>.Listen((data) =>
            {
                if (!data.player.isLocalPlayer) return;

                if (data.previous.pureKills != data.current.pureKills) PureKillIndicatorAnimation();
                else if (data.previous.finishingKills != data.current.finishingKills) UnpureKillIndicatorAnimation();
                else if (data.previous.supportingKills != data.current.supportingKills) UnpureKillIndicatorAnimation();
            });

            EventBus<OnPlayerHealthChanged>.Listen((data) =>
            {
                if (!data.healthModule.isLocalPlayer) return;
                if (data.newHealth >= data.oldHealth) return;
                DamageIndicatorAnimation();
            });

            EventBus<RespawnEffectRequest>.Listen((_) => TriggerRespawnEffect());
        }

        private Coroutine _respawnEffectCoroutine;
        private void TriggerRespawnEffect()
        {
            if (_respawnEffectCoroutine != null) StopCoroutine(_respawnEffectCoroutine);
            _respawnEffectCoroutine = StartCoroutine(nameof(CO_RespawnEffect));
        }

        private IEnumerator CO_RespawnEffect()
        {
            respawnEffect.gameObject.SetActive(true);

            var timer = 0f;
            while (timer < respawnEffectDuration)
            {
                respawnEffect.material.SetFloat("_BandHeight", timer / respawnEffectDuration * 3f - 1f);
                timer += Time.deltaTime;
                yield return null;
            }

            respawnEffect.gameObject.SetActive(false);
        }

        private Coroutine _damageIndicatorCoroutine;
        private Coroutine _pureKillIndicatorCoroutine;
        private Coroutine _unpureKillIndicatorCoroutine;

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

        private void OnDestroy()
        {
            respawnEffect.material.SetFloat("_BandHeight", -1f);
        }
    }
}