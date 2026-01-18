using Game.Core.Events;
using Game.Events.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Game
{
    public class DashCooldown : MonoBehaviour
    {
        public Slider dashCooldownSlider;
        private bool _cooldown;

        private void Awake()
        {
            EventBus<OnDash>.Listen((data) =>
            {
                dashCooldownSlider.maxValue = data.cooldown;
                dashCooldownSlider.value = data.cooldown;

                _cooldown = false;
            });

            EventBus<OnEndDash>.Listen((data) =>
            {
                if (data.reset)
                {
                    dashCooldownSlider.value = 0f;
                    return;
                }

                _cooldown = true;
            });
        }

        private void Update()
        {
            if (!_cooldown) return;

            dashCooldownSlider.value -= Time.deltaTime;
            if (dashCooldownSlider.value <= 0f) _cooldown = false;
        }
    }
}