using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Game.HitIndiction
{
    public class HitInfo : MonoBehaviour
    {
        public RectTransform rectTransform;
        public CanvasGroup canvasGroup;
        public Image typeGraphic;
        public TMP_Text otherInfo;

        [Header("Animation")]
        public float fadeDuration;
        public AnimationCurve fadeCurve;

        [Space(9)]
        public float endPosition;
        public float moveDuration;
        public AnimationCurve moveCurve;
        public float startInfoPosition;
        public float endInfoPosition;
        public float infoMoveDuration;
        public AnimationCurve infoMoveCurve;

        [Space(9)]
        public Vector3 startScale;
        public Vector3 endScale;
        public float startScaleDuration;
        public float endScaleDuration;
        public AnimationCurve startScaleCurve;
        public AnimationCurve endScaleCurve;

        private TweenerCore<float, float, FloatOptions> _fadeTween;
        private TweenerCore<Vector2, Vector2, VectorOptions> _moveTween;
        private TweenerCore<Vector2, Vector2, VectorOptions> _infoMoveTween;
        private TweenerCore<Vector3, Vector3, VectorOptions> _startScaleTween;
        private TweenerCore<Vector3, Vector3, VectorOptions> _endScaleTween;

        private void Awake()
        {
            canvasGroup.alpha = 0f;
        }

        public void Play(Sprite type, string info)
        {
            typeGraphic.sprite = type;
            otherInfo.text = info;

            _fadeTween?.Kill();
            _moveTween?.Kill();
            _infoMoveTween?.Kill();
            _startScaleTween?.Kill();
            _endScaleTween?.Kill();

            canvasGroup.alpha = 1f;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localScale = startScale;
            otherInfo.rectTransform.anchoredPosition = Vector2.up * startInfoPosition;

            _fadeTween = canvasGroup.DOFade(0f, fadeDuration).SetEase(fadeCurve);

            _moveTween = rectTransform.DOAnchorPosY(endPosition, moveDuration).SetEase(moveCurve);
            _infoMoveTween = otherInfo.rectTransform.DOAnchorPosY(endInfoPosition, infoMoveDuration).SetEase(infoMoveCurve);

            _startScaleTween = rectTransform.DOScale(Vector3.one, startScaleDuration).SetEase(startScaleCurve).OnComplete(() =>
            {
                _endScaleTween = rectTransform.DOScale(endScale, endScaleDuration).SetEase(endScaleCurve);
            });
        }
    }
}