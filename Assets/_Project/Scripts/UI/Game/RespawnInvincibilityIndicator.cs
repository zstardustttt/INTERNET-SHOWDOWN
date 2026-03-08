using Game.Core.Events;
using Game.Player.Death;
using Game.Player.Events;
using UnityEngine;

namespace Game.UI.Game
{
    public class RespawnInvincibilityIndicator : MonoBehaviour
    {
        public CanvasGroup canvasGroup;

        [Space(9)]
        public float awakeMinAlpha;
        public float awakeMaxAlpha;
        public float awakeInterval;

        [Space(9)]
        public float endingMinAlpha;
        public float endingMaxAlpha;
        public float endingInterval;

        private RespawnInvincibilityState _state;

        private void Awake()
        {
            canvasGroup.alpha = 0f;

            EventBus<OnPlayerRespawnInvincibilityStateChanged>.Listen((data) =>
            {
                if (!data.player.isLocalPlayer) return;
                _state = data.state;

                if (_state == RespawnInvincibilityState.None)
                    canvasGroup.alpha = 0f;
            });
        }

        private void Update()
        {
            if (_state == RespawnInvincibilityState.None) return;

            canvasGroup.alpha = _state switch
            {
                RespawnInvincibilityState.Awoken => Mathf.Lerp(awakeMinAlpha, awakeMaxAlpha, (Mathf.Sin(Time.time * Mathf.PI / awakeInterval) + 1f) / 2f),
                RespawnInvincibilityState.Ending => Mathf.Lerp(endingMinAlpha, endingMaxAlpha, (Mathf.Sin(Time.time * Mathf.PI / endingInterval) + 1f) / 2f),
                _ => throw new($"Respawn state {_state} isn't supported!")
            };
        }
    }
}