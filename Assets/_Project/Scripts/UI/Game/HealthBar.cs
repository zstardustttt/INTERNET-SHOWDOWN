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
        public Color healColor;

        [Header("Animation")]
        public float healthChangeSpeed;
        public float healColorFadeSpeed;

        [Header("Shake")]
        public float shakeAmplitude;
        public float shakeFrequency;
        public float shakeFalloffSpeed;

        private ShakeGenerator _shakeGenerator;
        private float _targetHealth;
        private float _healColorAffection;

        private void Awake()
        {
            _shakeGenerator = new();
            EventBus<OnHealthUpdate>.Listen(OnHealthUpdate);
        }

        private void OnHealthUpdate(OnHealthUpdate data)
        {
            if (data.health < _targetHealth)
                _shakeGenerator.Shake(shakeAmplitude, shakeFrequency, shakeFalloffSpeed);
            else
                _healColorAffection = 1f;

            healthBarSlider.maxValue = data.maxHealth;
            _targetHealth = data.health;
        }

        private void Update()
        {
            healthBarSlider.value = Mathf.Lerp(healthBarSlider.value, _targetHealth, Time.deltaTime * healthChangeSpeed);
            var rawFillColor = Color.Lerp(minHealthColor, maxHealthColor, healthBarSlider.value / healthBarSlider.maxValue);
            healthFillGraphic.color = Color.Lerp(rawFillColor, healColor, _healColorAffection);
            _healColorAffection = Mathf.Clamp01(_healColorAffection - Time.deltaTime * healColorFadeSpeed);

            var shake = _shakeGenerator.GetShake();
            healthBarShake.anchoredPosition = shake;
        }
    }
}