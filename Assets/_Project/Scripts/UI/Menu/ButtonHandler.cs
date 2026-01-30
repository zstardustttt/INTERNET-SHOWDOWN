using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ButtonHandler : MonoBehaviour
{
    public Vector2 shadowPos;
    public AnimationCurve[] curve;
    private Shadow _shadow;
    private Coroutine _mouseEnter;
    private Coroutine _mouseExit;
    private void Start()
    {
        _shadow = this.GetComponent<Shadow>();
    }

    public void MouseEnter()
    {
        StopAllCoroutines();
        _mouseEnter = StartCoroutine(AnimateShadow(_shadow, 0));
    }

    public void MouseExit()
    {
        StopAllCoroutines();
        _mouseExit = StartCoroutine(AnimateShadow(_shadow, 1));
    }

    private IEnumerator AnimateShadow(
        Shadow shadow,
        int curveIndex = 0,
        float duration = 0.3f
        )
    {
        AnimationCurve _curve = curve[curveIndex];

        float _value;
        float _timeNormalized;
        float _endKeyValue = _curve.keys[^1].value;
        float _elapsedTime = 0f;

        while (_elapsedTime < duration)
        {
            _elapsedTime += Time.deltaTime;
            _timeNormalized = _elapsedTime / duration;
            _value = _curve.Evaluate(_timeNormalized);

            shadow.effectDistance = new Vector2(_value * shadowPos.x, _value * shadowPos.y);

            if (_elapsedTime >= 2)
                _elapsedTime = 0;

            yield return null;
        }

        shadow.effectDistance = new Vector2(_endKeyValue * shadowPos.x, _endKeyValue * shadowPos.y);
    }
}
