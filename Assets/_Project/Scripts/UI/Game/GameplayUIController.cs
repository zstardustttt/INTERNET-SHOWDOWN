using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Events;
using Game.Events.GameLoop;
using Game.Events.UI;
using Game.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Game
{
    public struct GuidItemPair
    {
        public string guid;
        public LeaderboardItem item;
    }

    public struct LeaderboardItem
    {
        public string playerName;
        public int activity;
        public int score;
    }

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
        private Dictionary<string, LeaderboardItem> _unsortedLeaderboard;
        private List<LeaderboardItem> _leaderboardToDisplay;

        private void Awake()
        {
            _unsortedLeaderboard = new();
            _leaderboardToDisplay = new();

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
                _unsortedLeaderboard.Clear();
                _leaderboardToDisplay.Clear();
                ClearLeaderboardUI();
            });

            EventBus<PopulateLeaderboard>.Listen((data) =>
            {
                foreach (var pair in data.items)
                {
                    if (string.IsNullOrEmpty(pair.guid)) return;
                    if (_unsortedLeaderboard.TryAdd(pair.guid, pair.item))
                    {
                        _leaderboardToDisplay.Add(pair.item);
                    }
                }

                RefreshLeaderboardUI();
            });

            EventBus<ChangeLeaderboardItem>.Listen((data) =>
            {
                if (string.IsNullOrEmpty(data.guid)) return;
                _unsortedLeaderboard[data.guid] = data.item;
                _leaderboardToDisplay = _unsortedLeaderboard.Values.ToList();

                RefreshLeaderboardUI();
            });
        }

        private void ClearLeaderboardUI()
        {
            foreach (RectTransform item in leaderboard)
            {
                Destroy(item.gameObject);
            }
        }

        private void RefreshLeaderboardUI()
        {
            ClearLeaderboardUI();

            _leaderboardToDisplay.Sort((first, second) =>
            {
                if (first.score == second.score)
                {
                    if (first.activity < second.activity) return 1;
                    else return -1;
                }
                else if (first.score < second.score) return 1;
                else return -1;
            });

            for (int i = 0; i < _leaderboardToDisplay.Count; i++)
            {
                var item = _leaderboardToDisplay[i];
                var place = i + 1;
                var tmpText = Instantiate(leaderboardItemPrefab.gameObject, leaderboard).GetComponent<TMP_Text>();
                tmpText.text = $"{place}. {item.playerName}: {item.score}";
            }
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