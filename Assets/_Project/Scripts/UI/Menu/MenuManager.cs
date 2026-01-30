using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Objects")]
    public GameObject logo;
    public GameObject logoOutline;
    public GameObject mainPanel;
    public GameObject star;
    public GameObject[] buttons;
    private RectTransform _starTransform;
    private RectTransform _logoTransform;
    private RectTransform _logoOutlineTransform;
    private RawImage _logoOutlineRawImage;

    [Header("Settings")]
    public Slider sensitivity;

    [Header("Animation")]
    public AnimationCurve[] curves;

    private bool _animation;
    private bool _init;
    private Coroutine a;
    private void Awake()
    {
        _logoTransform = logo.GetComponent<RectTransform>();
        _logoOutlineTransform = logoOutline.GetComponent<RectTransform>();
        _logoOutlineRawImage = logoOutline.GetComponent<RawImage>();
        _starTransform = star.GetComponent<RectTransform>();

        _logoTransform.localScale = new Vector3(
            0f,
            0f,
            1f);

        if (PlayerPrefs.HasKey("sens"))
        {
            sensitivity.value = PlayerPrefs.GetFloat("sens");
            return;
        }

        PlayerPrefs.SetFloat("sens", 1f);
    }

    public void SetSens(float value)
    {
        PlayerPrefs.SetFloat("sens", value);
        PlayerPrefs.Save();
    }
    private void Start()
    {
        mainPanel.SetActive(false);
        StartCoroutine(AnimateScale(_logoTransform, Animate.XY, false, 1, 0.4f));
        a = StartCoroutine(AnimatePosRelative(_logoTransform, Animate.XY, true, 3, 1f));
    }

    private void Update()
    {
        _logoOutlineRawImage.uvRect = new Rect(
                    _logoOutlineRawImage.uvRect.x - 0.05f * Time.deltaTime,
                    _logoOutlineRawImage.uvRect.y + 0.05f * Time.deltaTime,
                    1f,
                    1f);

        _starTransform.localEulerAngles = new Vector3(
            _starTransform.localEulerAngles.x,
            35 + math.sin(Time.time / 2) * 10,
            _starTransform.localEulerAngles.z + 10f * Time.deltaTime
        );

        if (_init == false)
        {
            InputSystem.onAnyButtonPress
                .CallOnce(btn =>
                {
                    StopCoroutine(a);
                    mainPanel.SetActive(true);
                    StartCoroutine(AnimateScale(_logoTransform, Animate.Y, false, 2, 0.2f));
                    _init = true;
                });
            return;
        }

    }

    // Порнушный но рабочий рофл для анимаций
    private IEnumerator AnimateScale(
        RectTransform transform,
        Animate animOnly,
        bool continuous = false,
        int curveIndex = 0,
        float duration = 1f
        )
    {
        AnimationCurve _curve = curves[curveIndex];

        float _value;
        float _timeNormalized;
        float _endKeyValue = _curve.keys[^1].value;
        float _elapsedTime = 0f;

        while (_elapsedTime < duration || continuous)
        {
            _elapsedTime += Time.deltaTime;
            _timeNormalized = _elapsedTime / duration;
            _value = _curve.Evaluate(_timeNormalized);

            transform.localScale = new Vector3(
                (animOnly == Animate.X || animOnly == Animate.XY) ? _value : 1f,
                (animOnly == Animate.Y || animOnly == Animate.XY) ? _value : 1f,
                1f);

            if (_elapsedTime >= 2)
                _elapsedTime = 0;

            yield return null;
        }

        transform.localScale = new Vector3(_endKeyValue, _endKeyValue, 1f);
    }

    private IEnumerator AnimatePosRelative(
        RectTransform transform,
        Animate animOnly,
        bool continuous = false,
        int curveIndex = 0,
        float duration = 1f
        )
    {
        AnimationCurve _curve = curves[curveIndex];

        Vector3 _oldPosition = transform.localPosition;
        float _value;
        float _timeNormalized;
        float _endKeyValue = _curve.keys[^1].value;
        float _elapsedTime = 0f;

        while (_elapsedTime < duration || continuous)
        {
            _elapsedTime += Time.deltaTime;
            _timeNormalized = _elapsedTime / duration;
            _value = _curve.Evaluate(_timeNormalized);

            transform.localPosition = _oldPosition +
                new Vector3(
                    (animOnly == Animate.X || animOnly == Animate.XY) ? _value : 1f,
                    (animOnly == Animate.Y || animOnly == Animate.XY) ? _value : 1f,
                    0f);

            if (_elapsedTime >= 2)
                _elapsedTime = 0;

            yield return null;
        }

        transform.localScale = new Vector3(_endKeyValue, _endKeyValue, 1f);
    }

    private enum Animate : int
    {
        XYZ,
        XY,
        X,
        Y,
        Z,
    }
}