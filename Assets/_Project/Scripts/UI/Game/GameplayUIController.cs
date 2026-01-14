using System.Collections;
using Game.Core.Events;
using Game.Events.GameLoop;
using Game.Events.UI;
using Game.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Game
{
    public class GameplayUIController : MonoBehaviour
    {
        public CanvasGroup canvasGroup;
        public Slider health;
        public CanvasGroup hitIndicator;
        public CanvasGroup damageIndicator;
        public RectTransform leaderboard;
        public TMP_Text leaderboardItemPrefab;

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
            EventBus<HitIndicatorRequest>.Listen((_) => HitIndicatorAnimation());
            EventBus<DamageIndicatorRequest>.Listen((_) => DamageIndicatorAnimation());

            EventBus<ClearLeaderboard>.Listen((_) =>
            {
                foreach (RectTransform item in leaderboard)
                {
                    Destroy(item.gameObject);
                }
            });

            EventBus<AddToLeaderboard>.Listen((data) =>
            {
                var item = Instantiate(leaderboardItemPrefab.gameObject, leaderboard);
                item.GetComponent<TMP_Text>().text = $"{data.name} direct: {data.directHits} indirect: {data.indirectHits}";
            });
        }

        private void HitIndicatorAnimation()
        {
            StopCoroutine(nameof(CO_HitIndicatorAnimation));
            StartCoroutine(nameof(CO_HitIndicatorAnimation));
        }

        private void DamageIndicatorAnimation()
        {
            StopCoroutine(nameof(CO_DamageIndicatorAnimation));
            StartCoroutine(nameof(CO_DamageIndicatorAnimation));
        }

        private IEnumerator CO_HitIndicatorAnimation()
        {
            hitIndicator.alpha = 1f;
            while (hitIndicator.alpha > 0f)
            {
                hitIndicator.alpha -= Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator CO_DamageIndicatorAnimation()
        {
            damageIndicator.alpha = 1f;
            while (damageIndicator.alpha > 0f)
            {
                damageIndicator.alpha -= Time.deltaTime;
                yield return null;
            }
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