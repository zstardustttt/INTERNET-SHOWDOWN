using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TransitionManager : MonoBehaviour
{
    public static event Action OnTransitionInComplete;
    public GameObject star;
    private Material _material;

    private void Awake()
    {
        _material = star.GetComponent<RawImage>().material;
        star.SetActive(false);
        DontDestroyOnLoad(gameObject);
    }

    public Sequence TransitionIn()
    {
        var sequence = DOTween.Sequence();
        _material.DOKill(false);
        star.SetActive(true);
        sequence.Append(_material.DOFloat(2.5f, "_Size", 0.8f));
        return sequence;
    }

    public void TransitionOut()
    {
        _material.DOKill(false);
        _material.DOFloat(2.5f, "_InsetRadius", 0.8f)
            .SetDelay(1f)
            .OnComplete(() =>
            {
                star.SetActive(false);
                _material.SetFloat("_Size", 0);
                _material.SetFloat("_InsetRadius", 0);
            });

    }
}