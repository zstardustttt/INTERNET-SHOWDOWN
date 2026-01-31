using DG.Tweening;
using UnityEngine;

public class SectionManager : MonoBehaviour
{
    public Vector2[] pivotPositions;
    public Vector3[] rotations;
    private GameObject[] _sectionButtons;
    private MenuManager _menuManager;
    private RectTransform _mainPanel;
    private bool _sectionChanging = false;
    private Section _currentSection;
    private int _currentSubSection;
    public bool SectionChanging => _sectionChanging;
    public Section CurrentSection => _currentSection;
    public int currentSubSection => _currentSubSection;

    public void Awake()
    {
        _menuManager = FindObjectsByType<MenuManager>(FindObjectsSortMode.InstanceID)[0];
        _mainPanel = _menuManager.mainPanel.GetComponent<RectTransform>();

        var tempSectionButtons = FindObjectsByType<SectionButtonHandler>(FindObjectsSortMode.None);
        _sectionButtons = new GameObject[tempSectionButtons.Length];
        var i = 0;
        foreach (SectionButtonHandler sbh in tempSectionButtons)
        {
            _sectionButtons[i] = sbh.gameObject;
            i++;
        }
    }
    public void Transition(Section section)
    {
        _mainPanel.DORewind();
        _sectionChanging = true;
        switch (section)
        {
            case Section.MainScreen:
                _mainPanel.DOPivot(pivotPositions[0], 0.75f).SetEase(Ease.OutExpo);
                _mainPanel.DOScale(1f, 1f).SetEase(Ease.OutQuart);
                _mainPanel.DOLocalRotate(rotations[0], 1f).SetEase(Ease.OutQuart);
                return;
            case Section.PlayScreen:
                _mainPanel.DOPivot(pivotPositions[1], 0.75f).SetEase(Ease.OutExpo);
                _mainPanel.DOScale(2.75f, 1f).SetEase(Ease.OutQuart);
                _mainPanel.DOLocalRotate(rotations[1], 1f).SetEase(Ease.OutQuart);
                return;
            case Section.ProfileScreen:
                _mainPanel.DOPivot(pivotPositions[2], 0.75f).SetEase(Ease.OutExpo);
                _mainPanel.DOScale(2.75f, 1f).SetEase(Ease.OutQuart);
                _mainPanel.DOLocalRotate(rotations[2], 1f).SetEase(Ease.OutQuart);
                return;
            case Section.SettingsScreen:
                _mainPanel.DOPivot(pivotPositions[3], 0.75f).SetEase(Ease.OutExpo);
                _mainPanel.DOScale(2.75f, 1f).SetEase(Ease.OutQuart);
                _mainPanel.DOLocalRotate(rotations[3], 1f).SetEase(Ease.OutQuart);
                return;
            case Section.AutismScreen:
                _mainPanel.DOPivot(pivotPositions[4], 0.75f).SetEase(Ease.OutExpo);
                _mainPanel.DOScale(2.75f, 1f).SetEase(Ease.OutQuart);
                _mainPanel.DOLocalRotate(rotations[4], 1f).SetEase(Ease.OutQuart);
                return;
        }
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