using System.Collections;
using DG.Tweening;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class SectionButtonHandler : MonoBehaviour
{
    public Vector2 shadowPos;
    private MenuManager _menuManager;
    private SectionManager _sectionManager;
    private Shadow _shadow;
    private Tween _mouseEnter;
    private Tween _mouseExit;

    private void Awake()
    {
        _menuManager = FindObjectsByType<MenuManager>(FindObjectsSortMode.None)[0];
        _sectionManager = _menuManager.GetComponent<SectionManager>();
    }

    private void Start()
    {
        _shadow = this.GetComponent<Shadow>();
    }

    public void MouseEnter()
    {
        if (!_sectionManager.SectionChanging)
        {
            DOTween.Rewind(_mouseEnter);
            DOTween.Rewind(_mouseExit);
            _mouseEnter = DOTween.To(
                () => _shadow.effectDistance,
                x => _shadow.effectDistance = x,
                shadowPos,
                0.5f
            );
        }
    }

    public void MouseExit()
    {
        DOTween.Rewind(_mouseEnter);
        DOTween.Rewind(_mouseExit);
        _mouseEnter = DOTween.To(
            () => _shadow.effectDistance,
            x => _shadow.effectDistance = x,
            Vector2.zero,
            0.5f
        );
    }

    public void MouseClick(int section)
    {
        if (!_sectionManager.SectionChanging || _sectionManager.CurrentSection != SectionManager.Section.MainScreen)
        {
            switch (section)
            {
                case 0:
                    _sectionManager.Transition(SectionManager.Section.MainScreen);
                    return;
                case 1:
                    _sectionManager.Transition(SectionManager.Section.PlayScreen);
                    return;
                case 2:
                    _sectionManager.Transition(SectionManager.Section.ProfileScreen);
                    return;
                case 3:
                    _sectionManager.Transition(SectionManager.Section.SettingsScreen);
                    return;
                case 4:
                    _sectionManager.Transition(SectionManager.Section.AutismScreen);
                    return;
                default:
                    return;
            }
        }
    }
}
