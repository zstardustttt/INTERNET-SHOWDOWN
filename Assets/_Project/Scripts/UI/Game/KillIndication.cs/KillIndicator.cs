using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Game.Core.Events;
using Game.Core.Player.Stats;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Game.KillIndication
{
    public class KillIndicator : MonoBehaviour
    {
        public const float SEMITONE = 1f / 12f;

        public RectTransform rectTransform;
        public Vector3 startScale;
        public float scaleDuration;
        public AnimationCurve scaleEase;

        [Space(9)]
        public CanvasGroup canvasGroup;
        public float fadeDuration;
        public AnimationCurve fadeEase;

        [Space(9)]
        public Image pureKillGraphic;
        public float pureKillColorDuration;
        public AnimationCurve pureKillColorEase;

        [Space(9)]
        public RectTransform pureKillShines;
        public Vector3 startPureKillShinesScale;
        public float pureKillShinesScaleDuration;
        public AnimationCurve pureKillShinesScaleEase;

        [Space(9)]
        public CanvasGroup pureKillShinesCanvasGroup;
        public float pureKillShinesFadeInDuration;
        public float pureKillShinesFadeOutDuration;
        public AnimationCurve pureKillShinesFadeInEase;
        public AnimationCurve pureKillShinesFadeOutEase;

        [Space(9)]
        public RectTransform pureKillShineInner;
        public RectTransform pureKillShineOuter;
        public float startPureKillShineRotationSpeed;
        public float endPureKillShineRotationSpeed;
        public float pureKillShineRotationFadeSpeed;

        [Space(9)]
        public AudioSource audioSource;
        public float comboDuration;
        public float idlePitch;
        public float maxPitch;

        private TweenerCore<Vector3, Vector3, VectorOptions> _scaleTween;
        private TweenerCore<float, float, FloatOptions> _fadeTween;
        private TweenerCore<Color, Color, ColorOptions> _colorTween;
        private TweenerCore<Vector3, Vector3, VectorOptions> _shinesScaleTween;
        private TweenerCore<float, float, FloatOptions> _shinesFadeTween;
        private float _pureKillShineRotationSpeed;
        private float _comboTimer;

        private void Awake()
        {
            audioSource.pitch = idlePitch;
            canvasGroup.alpha = 0f;

            EventBus<OnPlayerStatsChanged>.Listen((data) =>
            {
                if (!data.player.isLocalPlayer) return;

                if (data.previous.pureKills < data.current.pureKills) Play();
                else if (data.previous.finishingKills < data.current.finishingKills) Play();
                else if (data.previous.supportingKills < data.current.supportingKills) Play();
            });
        }

        private void Update()
        {
#if DEBUG
            if (Input.GetKeyDown(KeyCode.F7))
            {
                Play();
            }
#endif

            if (_comboTimer < 0f)
            {
                audioSource.pitch = idlePitch;
                _comboTimer = 0f;
            }
            else if (_comboTimer > 0f) _comboTimer -= Time.deltaTime;

            _pureKillShineRotationSpeed = Mathf.Lerp(_pureKillShineRotationSpeed, endPureKillShineRotationSpeed, Time.deltaTime * pureKillShineRotationFadeSpeed);

            pureKillShineInner.localEulerAngles += 0.35f * _pureKillShineRotationSpeed * Time.deltaTime * Vector3.forward;
            pureKillShineOuter.localEulerAngles += _pureKillShineRotationSpeed * Time.deltaTime * Vector3.forward;
        }

        private void Play()
        {
            _scaleTween?.Kill();
            _fadeTween?.Kill();
            _colorTween?.Kill();
            _shinesScaleTween?.Kill();
            _shinesFadeTween?.Kill();

            rectTransform.localScale = startScale;
            canvasGroup.alpha = 1f;
            pureKillGraphic.color = Color.red;
            pureKillShines.localScale = startPureKillShinesScale;
            pureKillShinesCanvasGroup.alpha = 0f;
            _pureKillShineRotationSpeed = startPureKillShineRotationSpeed;

            _scaleTween = rectTransform.DOScale(Vector3.one, scaleDuration).SetEase(scaleEase);
            _fadeTween = canvasGroup.DOFade(0f, fadeDuration).SetEase(fadeEase);
            _colorTween = pureKillGraphic.DOColor(Color.white, pureKillColorDuration).SetEase(pureKillColorEase);
            _shinesScaleTween = pureKillShines.DOScale(Vector3.one, pureKillShinesScaleDuration).SetEase(pureKillShinesScaleEase);
            _shinesFadeTween = pureKillShinesCanvasGroup.DOFade(1f, pureKillShinesFadeInDuration).SetEase(pureKillShinesFadeInEase).OnComplete(() =>
            {
                _shinesFadeTween = pureKillShinesCanvasGroup.DOFade(0f, pureKillShinesFadeOutDuration).SetEase(pureKillShinesFadeOutEase);
            });

            audioSource.pitch = Mathf.Min(audioSource.pitch + SEMITONE, maxPitch);
            audioSource.Play();
            _comboTimer = comboDuration;
        }
    }
}