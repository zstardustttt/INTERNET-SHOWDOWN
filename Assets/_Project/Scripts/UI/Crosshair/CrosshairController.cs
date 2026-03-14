using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Game.Core.Damages;
using Game.Core.Events;
using Game.Player.Online.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Game.Crosshair
{
    public class CrosshairController : MonoBehaviour
    {
        public Image crosshairGraphic;
        public RectTransform crosshairRect;

        [Space(9)]
        public Color hitColor;
        public float hitColorDuration;
        public AnimationCurve hitColorCurve;

        [Space(9)]
        public Vector3 hitScale;
        public float hitScaleDuration;
        public AnimationCurve hitScaleCurve;

        private Color _idleColor;
        private TweenerCore<Color, Color, ColorOptions> _hitColorTween;
        private TweenerCore<Vector3, Vector3, VectorOptions> _hitScaleTween;

        [Space(9)]
        public AudioSource hitSource;

        private void Awake()
        {
            _idleColor = crosshairGraphic.color;

            EventBus<OnLocalPlayerDealtDamage>.Listen((data) =>
            {
                if (data.type != DamageType.Direct) return;
                DirectHitAnimation();
            });
        }

        private void DirectHitAnimation()
        {
            _hitColorTween?.Kill();
            _hitScaleTween?.Kill();

            crosshairGraphic.color = hitColor;
            crosshairRect.localScale = hitScale;

            _hitColorTween = crosshairGraphic.DOColor(_idleColor, hitColorDuration).SetEase(hitColorCurve);
            _hitScaleTween = crosshairRect.DOScale(Vector3.one, hitScaleDuration).SetEase(hitScaleCurve);

            hitSource.Play();
        }

#if DEBUG
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
                DirectHitAnimation();
        }
#endif
    }
}