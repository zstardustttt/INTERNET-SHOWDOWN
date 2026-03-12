using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Game.Core.Events;
using Game.Events.UI;
using TMPro;
using UnityEngine;

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

    public class Leaderboard : MonoBehaviour
    {
        public RectTransform leaderboardContainer;
        public TMP_Text leaderboardItemPrefab;

        private Dictionary<Guid, LeaderboardItem> _virtualLeaderboard;
        private Dictionary<Guid, TMP_Text> _displayedLeaderboard;

        private void Awake()
        {
            _virtualLeaderboard = new();
            _displayedLeaderboard = new();

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
                var tmpText = Instantiate(leaderboardItemPrefab.gameObject, leaderboardContainer).GetComponent<TMP_Text>();
                _displayedLeaderboard.Add(guid, tmpText);
                tmpText.text = GetLeaderboardItemUIText(item, place);
                place++;
            }
        }

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
    }
}