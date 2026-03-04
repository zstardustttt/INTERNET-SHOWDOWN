using Game.Core.Events;
using Game.Player.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Game
{
    public class DashCooldown : MonoBehaviour
    {
        public Slider[] dashCooldownSliders;
        private bool _cooldownActive;
        private float _cooldown;

        private void Awake()
        {
            EventBus<OnDash>.Listen((data) =>
            {
                foreach (var slider in dashCooldownSliders)
                {
                    slider.maxValue = data.cooldown;
                    slider.value = data.cooldown;
                }

                _cooldown = data.cooldown;
                _cooldownActive = false;
            });

            EventBus<OnEndDash>.Listen((data) =>
            {
                if (data.reset)
                {
                    foreach (var slider in dashCooldownSliders)
                    {
                        slider.value = 0f;
                    }

                    _cooldown = 0f;
                    return;
                }

                _cooldownActive = true;
            });
        }

        private void Update()
        {
            if (!_cooldownActive) return;

            _cooldown -= Time.deltaTime;
            _cooldown = Mathf.Max(0f, _cooldown);
            if (_cooldown <= 0f) _cooldownActive = false;

            foreach (var slider in dashCooldownSliders)
            {
                slider.value = _cooldown;
            }
        }
    }
}