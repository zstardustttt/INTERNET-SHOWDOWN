using DG.Tweening;
using EasyTextEffects;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ButtonHandler : MonoBehaviour
{
    public int subsection;
    private RawImage _rawImage;
    private Material _material;
    private MenuManager _menuManager;
    private SectionManager _sectionManager;
    private Sequence _mouseEnter;
    private Sequence _mouseExit;
    private void Awake()
    {
        _rawImage = GetComponent<RawImage>();
        _material = Instantiate(_rawImage.material);
        _rawImage.material = _material;
        _menuManager = FindObjectsByType<MenuManager>(FindObjectsSortMode.None)[0];
        _sectionManager = _menuManager.GetComponent<SectionManager>();
    }

    public void SubsectionChange(int nextSubsection)
    {
        if (nextSubsection != subsection)
        {
            DOTween.Kill(_material);
            DOTween.Sequence()
                .Append(_material.DOFloat(1.5f, "_GradientIntensity", 0.5f))
                .Join(_material.DOFloat(0.5f, "_EffectIntensity", 0.4f));
        }
        else
        {
            MouseExit();
        }
    }

    public void MouseEnter()
    {
        if (_sectionManager.CurrentSubSection == subsection)
        {
            DOTween.Kill(_material);
            _mouseEnter = DOTween.Sequence();
            _mouseEnter
                .Append(_material.DOFloat(4f, "_GradientIntensity", 0.5f))
                .Join(_material.DOFloat(0.5f, "_EffectIntensity", 0.4f));
        }
    }

    public void MouseExit()
    {
        if (_sectionManager.CurrentSubSection == subsection)
        {
            DOTween.Kill(_material);
            _mouseExit = DOTween.Sequence();
            _mouseExit
                .Append(_material.DOFloat(2f, "_GradientIntensity", 0.5f))
                .Join(_material.DOFloat(0f, "_EffectIntensity", 0.4f));
        }
    }

    public void OnDestroy()
    {
        Destroy(_material);
    }
}
