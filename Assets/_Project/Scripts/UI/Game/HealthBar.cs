using Game.Core.Events;
using Game.Events.UI;
using Game.Other;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Game
{
    public class HealthBar : MonoBehaviour
    {
        [Header("Objects")]
        public RectTransform healthBarShake;
        public Slider healthBarSlider;
        public Graphic healthFillGraphic;

        [Header("Color")]
        public Color maxHealthColor;
        public Color minHealthColor;

        [Header("Animation")]
        public float healthChangeSpeed;

        [Header("Shake")]
        public float shakeAmplitude;
        public float shakeFrequency;
        public float shakeFalloffSpeed;

        private ShakeGenerator _shakeGenerator;
        private float _targetHealth;

        private void Awake()
        {
            _shakeGenerator = new();
            EventBus<OnHealthUpdate>.Listen(OnHealthUpdate);
        }

        private void OnHealthUpdate(OnHealthUpdate data)
        {
            if (data.health < _targetHealth)
                _shakeGenerator.Shake(shakeAmplitude, shakeFrequency, shakeFalloffSpeed);

            healthBarSlider.maxValue = data.maxHealth;
            _targetHealth = data.health;
        }

        private void Update()
        {
            healthBarSlider.value = Mathf.Lerp(healthBarSlider.value, _targetHealth, Time.deltaTime * healthChangeSpeed);
            healthFillGraphic.color = Color.Lerp(minHealthColor, Color.white, healthBarSlider.value / healthBarSlider.maxValue);

            var shake = _shakeGenerator.GetShake();
            healthBarShake.anchoredPosition = shake;
        }
    }
}