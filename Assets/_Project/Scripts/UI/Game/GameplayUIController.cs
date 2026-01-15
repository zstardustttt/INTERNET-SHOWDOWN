using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Game.Core.Events;
using Game.Events.GameLoop;
using Game.Events.UI;
using Game.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Game
{
    public struct LeaderboardItem
    {
        public string name;
        public int activity;
        public int score;
    }

    public struct LeaderboardComparer : IComparer<LeaderboardItem>
    {
        public readonly int Compare(LeaderboardItem x, LeaderboardItem y)
        {
            if (x.score == y.score)
            {
                if (x.activity < y.activity) return 1;
                else return -1;
            }
            else if (x.score < y.score) return 1;
            else return -1;
        }
    }

    public class GameplayUIController : MonoBehaviour
    {
        public CanvasGroup canvasGroup;
        public Slider health;
        public CanvasGroup hitIndicator;
        public CanvasGroup damageIndicator;
        public CanvasGroup pureKillIndicator;
        public CanvasGroup unpureKillIndicator;
        public RectTransform leaderboard;
        public TMP_Text leaderboardItemPrefab;

        private GameState _gameState;
        private bool _uiSwitchRequested;
        private Dictionary<string, LeaderboardItem> _virtualLeaderboard;
        private Dictionary<string, TMP_Text> _displayedLeaderboard;

        private void Awake()
        {
            _virtualLeaderboard = new();
            _displayedLeaderboard = new();

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
            EventBus<PureKillIndicatorRequest>.Listen((_) => PureKillIndicatorAnimation());
            EventBus<UnpureKillIndicatorRequest>.Listen((_) => UnpureKillIndicatorAnimation());

            RegisterLeaderboardListeners();
        }

        private void RegisterLeaderboardListeners()
        {
            EventBus<ClearLeaderboard>.Listen(ClearLeaderboard);
            EventBus<PopulateLeaderboard>.Listen(PopulateLeaderboard);
            EventBus<AddLeaderboardItem>.Listen(AddLeaderboardItem);
            EventBus<RemoveLeaderboardItem>.Listen(RemoveLeaderboardItem);
            EventBus<ChangeLeaderboardItem>.Listen(ChangeLeaderboardItem);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetLeaderboardItemUIText(LeaderboardItem item, int place) => $"{place}. {item.name}: {item.score}";

        private void ClearDisplayedLeaderboard()
        {
            foreach (var (_, tmpText) in _displayedLeaderboard)
            {
                Destroy(tmpText.gameObject);
            }
            _displayedLeaderboard.Clear();
        }

        private void RecreateDisplayedLeaderboard()
        {
            ClearDisplayedLeaderboard();

            var place = 1;
            foreach (var (guid, item) in _virtualLeaderboard)
            {
                var tmpText = Instantiate(leaderboardItemPrefab.gameObject, leaderboard).GetComponent<TMP_Text>();
                _displayedLeaderboard.Add(guid, tmpText);
                tmpText.text = GetLeaderboardItemUIText(item, place);
                place++;
            }
        }

        // TODO: seperate leaderboard
        private void RefreshDisplayedLeaderboard()
        {
            var place = 1;
            foreach (var (guid, item) in _virtualLeaderboard)
            {
                _displayedLeaderboard[guid].text = GetLeaderboardItemUIText(item, place);
                place++;
            }
        }

        private void ClearLeaderboard(ClearLeaderboard data)
        {
            _virtualLeaderboard.Clear();
            ClearDisplayedLeaderboard();
        }

        private void PopulateLeaderboard(PopulateLeaderboard data)
        {
            foreach (var itemData in data.itemDatas)
            {
                if (!_virtualLeaderboard.TryAdd(itemData.guid, itemData.item))
                {
                    Debug.LogWarning($"Failed to add leaderboard item of guid: {itemData.guid} player name: {itemData.item.name}");
                    continue;
                }
            }

            _virtualLeaderboard = _virtualLeaderboard.OrderBy(x => x.Value, new LeaderboardComparer()).ToDictionary(k => k.Key, v => v.Value);
            RecreateDisplayedLeaderboard();
        }

        private void AddLeaderboardItem(AddLeaderboardItem data)
        {
            if (!_virtualLeaderboard.TryAdd(data.itemData.guid, data.itemData.item))
            {
                Debug.LogWarning($"Failed to add leaderboard item of guid: {data.itemData.guid} player name: {data.itemData.item.name}");
                return;
            }

            _virtualLeaderboard = _virtualLeaderboard.OrderBy(x => x.Value, new LeaderboardComparer()).ToDictionary(k => k.Key, v => v.Value);
            RecreateDisplayedLeaderboard();
        }

        private void RemoveLeaderboardItem(RemoveLeaderboardItem data)
        {
            if (!_virtualLeaderboard.Remove(data.guid))
            {
                Debug.LogWarning($"Failed to remove leaderboard item of guid: {data.guid}");
                return;
            }

            Destroy(_displayedLeaderboard[data.guid].gameObject);
            _displayedLeaderboard.Remove(data.guid);
            RefreshDisplayedLeaderboard();
        }

        private void ChangeLeaderboardItem(ChangeLeaderboardItem data)
        {
            if (!_virtualLeaderboard.ContainsKey(data.itemData.guid))
            {
                Debug.LogWarning($"Failed to change leaderboard item of guid: {data.itemData.guid}");
                return;
            }

            _virtualLeaderboard[data.itemData.guid] = data.itemData.item;
            _virtualLeaderboard = _virtualLeaderboard.OrderBy(x => x.Value, new LeaderboardComparer()).ToDictionary(k => k.Key, v => v.Value);
            RecreateDisplayedLeaderboard();
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

        private void OnHealthUpdate(OnHealthUpdate data)
        {
            health.maxValue = data.maxHealth;
            health.value = data.health;
        }
    }
}