using System;
using DG.Tweening;
using Mirror.BouncyCastle.Tls.Crypto.Impl.BC;
using UnityEngine;

public class SectionManager : MonoBehaviour
{
    [Header("Section Transforms")]
    [SerializeField] private Vector2[] pivotPositions;
    [SerializeField] private Vector3[] rotations;

    private GameObject[] _sectionButtons;
    private MenuManager _menuManager;
    private RectTransform _mainPanel;
    public ButtonContainerHandler[] buttonContainerHandler;

    private bool _sectionChanging;
    private Section _currentSection;
    private int _currentSubSection;

    public bool SectionChanging => _sectionChanging;
    public Section CurrentSection => _currentSection;
    public int CurrentSubSection => _currentSubSection;

    public void Awake()
    {
        _menuManager = FindObjectsByType<MenuManager>(FindObjectsSortMode.InstanceID)[0];
        _mainPanel = _menuManager.mainPanel.GetComponent<RectTransform>();
    }

    public void SubsectionChange(int subsect)
    {
        int index = (int)_currentSection - 1;
        Debug.Log(index);
        Debug.Log(buttonContainerHandler[index]);
        foreach (ButtonHandler b in buttonContainerHandler[index].buttonHandlers)
        {
            b.SubsectionChange(subsect);
        }

        var sequence = DOTween.Sequence();

        _currentSubSection = 1;
    }

    public void Transition(int i)
    {
        Transition((Section)i);
    }

    public void Transition(Section section)
    {
        int index = (int)section;

        _sectionChanging = true;
        _currentSection = section;

        float targetScale = section == Section.MainScreen ? 1f : 2.75f;

        Sequence sequence = DOTween.Sequence();

        if ((int)section != 0)
            buttonContainerHandler[Math.Max((int)section - 1, 0)].TransitionIn();
        else
            foreach (ButtonContainerHandler bch in buttonContainerHandler)
            {
                bch.TransitionOut();
            }

        var a = (int)section == 0 ? Ease.InOutSine : Ease.OutExpo;
        var b = (int)section == 0 ? Ease.OutExpo : Ease.OutQuart;

        sequence.Append(
            _mainPanel.DOPivot(pivotPositions[index], 0.75f)
                .SetEase(a)
        );

        sequence.Join(
            _mainPanel.DOScale(targetScale, 1f)
                .SetEase(b)
        );

        sequence.Join(
            _mainPanel.DOLocalRotate(rotations[index], 1f)
                .SetEase(b)
        );

        sequence.OnComplete(() =>
        {
            _sectionChanging = false;
            DOTween.Kill(this);
        });
    }
    public enum Section
    {
        MainScreen,
        PlayScreen,
        ProfileScreen,
        SettingsScreen,
        AutismScreen // я не придумал для чего последний квадрат сделать
    }
}
