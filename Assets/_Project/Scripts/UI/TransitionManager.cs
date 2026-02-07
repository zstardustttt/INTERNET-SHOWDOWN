using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TransitionManager : MonoBehaviour
{
    public GameObject star;
    private bool quit = false;
    private Material _material;

    private void Awake()
    {
        _material = star.GetComponent<RawImage>().material;
        star.SetActive(false);
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        Application.wantsToQuit += OnWantsToQuit;
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    private void OnDisable()
    {
        Application.wantsToQuit -= OnWantsToQuit;
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    private bool OnWantsToQuit()
    {
        if (quit)
            return true;

        TransitionIn()
            .OnComplete(() =>
            {
                quit = true;
                Application.Quit();
            })
            .OnKill(() =>
            {
                quit = true;
                Application.Quit();
            });

        return false;
    }

    private void OnSceneChanged(Scene current, Scene next)
    {
        TransitionOut();
    }
    public Sequence TransitionIn()
    {
        star.SetActive(true);
        var sequence = DOTween.Sequence();
        sequence.Append(_material.DOFloat(2.5f, "_Size", 1.0f).OnComplete(() =>
        {
            DOTween.Kill(this);
        }));

        return sequence;
    }

    public void TransitionOut()
    {
        _material.DOFloat(2.5f, "_InsetRadius", 1.0f)
            .SetDelay(1f)
            .OnComplete(() =>
            {
                star.SetActive(false);
                _material.SetFloat("_Size", 0);
                _material.SetFloat("_InsetRadius", 0);
                DOTween.Kill(this);
            });
    }

}