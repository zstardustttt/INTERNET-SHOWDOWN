using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Game.Core.Events;
using Game.Player.Events;
using UnityEngine;
using UnityEngine.UI;

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

            EventBus<OnPlayerStatsChanged>.Listen((data) =>
            {
                if (!data.player.isLocalPlayer) return;

                if (data.previous.directHits < data.current.directHits)
                    Indicate(data.current.GetScore() - data.previous.GetScore(), true);
                else if (data.previous.indirectHits < data.current.indirectHits)
                    Indicate(data.current.GetScore() - data.previous.GetScore(), false);
            });
        }

        private void Indicate(int scoreIncrease, bool direct)
        {
            _indicatorFadeTween?.Kill();
            indicatorGraphic.color = new(indicatorGraphic.color.r, indicatorGraphic.color.g, indicatorGraphic.color.b, indicatorStartAlpha);
            _indicatorFadeTween = indicatorGraphic.DOFade(0f, indicatorFadeDuration).SetEase(indicatorFadeEase);

            var sprite = direct ? directHitSprite : indirectHitSprite;
            hitInfo.Play(sprite, $"<color=yellow>+{scoreIncrease}</color> score");

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
                Indicate(Random.Range(0, 100), true);
            else if (Input.GetKeyDown(KeyCode.F6))
                Indicate(Random.Range(0, 100), false);
#endif
        }
    }
}