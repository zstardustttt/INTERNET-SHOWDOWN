using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Game.Core.Damages;
using Game.Core.Events;
using Game.Player.Online.Events;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Game.UI.Game.HitIndiction
{
    public class HitIndicator : MonoBehaviour
    {
        public const float SEMITONE = 1f / 12f;

        public Image indicatorGraphic;
        public float indicatorStartAlpha;
        public float indicatorFadeDuration;
        public AnimationCurve indicatorFadeEase;

        [Space(9)]
        public Sprite directHitSprite;
        public Sprite indirectHitSprite;
        public HitInfo hitInfo;

        [Space(9)]
        public AudioSource audioSource;
        public float comboDuration;
        public float idlePitch;
        public float maxPitch;

        private TweenerCore<Color, Color, ColorOptions> _indicatorFadeTween;
        private float _comboTimer;

        private void Awake()
        {
            audioSource.pitch = idlePitch;

            EventBus<OnLocalPlayerDealtDamage>.Listen((data) =>
            {
                Indicate(data.type, data.target.name, data.source.name, data.amount);
            });
        }

        private void Indicate(DamageType type, string playerName, string weaponName, float score)
        {
            _indicatorFadeTween?.Kill();
            indicatorGraphic.color = new(indicatorGraphic.color.r, indicatorGraphic.color.g, indicatorGraphic.color.b, indicatorStartAlpha);
            _indicatorFadeTween = indicatorGraphic.DOFade(0f, indicatorFadeDuration).SetEase(indicatorFadeEase);

            var sprite = type == DamageType.Direct ? directHitSprite : indirectHitSprite;
            hitInfo.Play(sprite, playerName, weaponName, (int)score);

            audioSource.pitch = Mathf.Min(audioSource.pitch + SEMITONE, maxPitch);
            audioSource.Play();
            _comboTimer = comboDuration;
        }

        private void Update()
        {
            if (_comboTimer < 0f)
            {
                audioSource.pitch = idlePitch;
                _comboTimer = 0f;
            }
            else if (_comboTimer > 0f) _comboTimer -= Time.deltaTime;

#if DEBUG
            if (Input.GetKeyDown(KeyCode.F5))
                Indicate(DamageType.Direct, "Player", "Ultra Item", Random.Range(0f, 100f));
            else if (Input.GetKeyDown(KeyCode.F6))
                Indicate(DamageType.Indirect, "Player", "Ultra Item", Random.Range(0f, 100f));
#endif
        }
    }
}