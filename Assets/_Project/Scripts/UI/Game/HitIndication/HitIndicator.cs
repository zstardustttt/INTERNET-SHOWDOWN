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
        public Image indicatorGraphic;
        public float indicatorStartAlpha;
        public float indicatorFadeDuration;
        public AnimationCurve indicatorFadeEase;

        [Space(9)]
        public Sprite directHitSprite;
        public Sprite indirectHitSprite;
        public HitInfo hitInfo;

        private TweenerCore<Color, Color, ColorOptions> _indicatorFadeTween;

        private void Awake()
        {
            EventBus<OnPlayerStatsChanged>.Listen((data) =>
            {
                if (!data.player.isLocalPlayer) return;

                if (data.previous.directHits != data.current.directHits)
                    Indicate(data.current.GetScore() - data.previous.GetScore(), true);
                else if (data.previous.indirectHits != data.current.indirectHits)
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
        }
    }
}