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
        public Image typeGraphic;
        public CanvasGroup typeCanvasGroup;
        public RectTransform infoRectTransform;
        public CanvasGroup infoCanvasGroup;

        [Space(9)]
        public TMP_Text playerText;
        public TMP_Text weaponText;
        public TMP_Text scoreText;

        [Header("Animation")]
        public float typeFadeDuration;
        public AnimationCurve typeFadeCurve;

        public float infoFadeDuration;
        public AnimationCurve infoFadeCurve;

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

        private TweenerCore<float, float, FloatOptions> _typeFadeTween;
        private TweenerCore<float, float, FloatOptions> _infoFadeTween;
        private TweenerCore<Vector2, Vector2, VectorOptions> _moveTween;
        private TweenerCore<Vector2, Vector2, VectorOptions> _infoMoveTween;
        private TweenerCore<Vector3, Vector3, VectorOptions> _startScaleTween;
        private TweenerCore<Vector3, Vector3, VectorOptions> _endScaleTween;

        private void Awake()
        {
            typeCanvasGroup.alpha = 0f;
            infoCanvasGroup.alpha = 0f;
        }

        public void Play(Sprite type, string playerName, string weaponName, int score)
        {
            typeGraphic.sprite = type;
            playerText.text = playerName;
            weaponText.text = weaponName;
            scoreText.text = score.ToString();

            _typeFadeTween?.Kill();
            _infoFadeTween?.Kill();
            _moveTween?.Kill();
            _infoMoveTween?.Kill();
            _startScaleTween?.Kill();
            _endScaleTween?.Kill();

            typeCanvasGroup.alpha = 1f;
            infoCanvasGroup.alpha = 1f;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localScale = startScale;
            infoRectTransform.anchoredPosition = Vector2.up * startInfoPosition;

            _typeFadeTween = typeCanvasGroup.DOFade(0f, typeFadeDuration).SetEase(typeFadeCurve);
            _infoFadeTween = infoCanvasGroup.DOFade(0f, infoFadeDuration).SetEase(infoFadeCurve);

            _moveTween = rectTransform.DOAnchorPosY(endPosition, moveDuration).SetEase(moveCurve);
            _infoMoveTween = infoRectTransform.DOAnchorPosY(endInfoPosition, infoMoveDuration).SetEase(infoMoveCurve);

            _startScaleTween = rectTransform.DOScale(Vector3.one, startScaleDuration).SetEase(startScaleCurve).OnComplete(() =>
            {
                _endScaleTween = rectTransform.DOScale(endScale, endScaleDuration).SetEase(endScaleCurve);
            });
        }
    }
}